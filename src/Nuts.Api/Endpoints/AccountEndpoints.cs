using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Nuts.Application.Account;
using Nuts.Application.Common;
using Nuts.Domain.Entities;

namespace Nuts.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/auth").WithTags("Account — Auth");
        var accountGroup = app.MapGroup("/api/account").WithTags("Account").RequireAuthorization("User");

        // ── Registration ──
        authGroup.MapPost("/register", async Task<Results<Ok<AuthResponse>, BadRequest<string>>> (
            RegisterRequest req,
            IUserRepository userRepo,
            IUnitOfWork uow,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var existing = await userRepo.GetByEmailAsync(req.Email.Trim().ToLowerInvariant(), ct);
            if (existing is not null)
                return TypedResults.BadRequest("Пользователь с таким e-mail уже существует.");

            var passwordHash = HashPassword(req.Password);
            var userResult = User.Create(req.Email, passwordHash, req.FullName, req.Phone);

            return await userResult.Match<Task<Results<Ok<AuthResponse>, BadRequest<string>>>>(
                async user =>
                {
                    userRepo.Add(user);
                    await uow.SaveChangesAsync(ct);
                    var token = GenerateToken(user, config);
                    return TypedResults.Ok(new AuthResponse(token, user.FullName, user.Email));
                },
                error => Task.FromResult<Results<Ok<AuthResponse>, BadRequest<string>>>(
                    TypedResults.BadRequest(error.Message)));
        }).AllowAnonymous();

        // ── Login ──
        authGroup.MapPost("/user-login", async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> (
            UserLoginRequest req,
            IUserRepository userRepo,
            IUnitOfWork uow,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByEmailAsync(req.Email.Trim().ToLowerInvariant(), ct);
            if (user is null)
                return TypedResults.Unauthorized();

            if (!VerifyPassword(req.Password, user.PasswordHash))
                return TypedResults.Unauthorized();

            user.UpdateLastLogin();
            await uow.SaveChangesAsync(ct);
            var token = GenerateToken(user, config);
            return TypedResults.Ok(new AuthResponse(token, user.FullName, user.Email));
        }).AllowAnonymous();

        // ── Profile ──
        accountGroup.MapGet("/profile", async Task<Results<Ok<ProfileResponse>, NotFound>> (
            ClaimsPrincipal claims,
            IUserRepository userRepo,
            CancellationToken ct) =>
        {
            var userId = GetUserId(claims);
            if (userId is null) return TypedResults.NotFound();

            var user = await userRepo.GetByIdAsync(userId.Value, ct);
            if (user is null) return TypedResults.NotFound();

            return TypedResults.Ok(new ProfileResponse(
                user.Id, user.FullName, user.Email, user.Phone,
                user.Address, user.AddressNote, user.CreatedAt));
        });

        accountGroup.MapPut("/profile", async Task<Results<Ok<ProfileResponse>, NotFound, BadRequest<string>>> (
            UpdateProfileRequest req,
            ClaimsPrincipal claims,
            IUserRepository userRepo,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var userId = GetUserId(claims);
            if (userId is null) return TypedResults.NotFound();

            var user = await userRepo.GetByIdAsync(userId.Value, ct);
            if (user is null) return TypedResults.NotFound();

            var result = user.UpdateProfile(req.FullName, req.Phone);
            if (result.IsFailure)
                return TypedResults.BadRequest(result.Error.Message);

            await uow.SaveChangesAsync(ct);
            return TypedResults.Ok(new ProfileResponse(
                user.Id, user.FullName, user.Email, user.Phone,
                user.Address, user.AddressNote, user.CreatedAt));
        });

        // ── Address ──
        accountGroup.MapPut("/address", async Task<Results<Ok, NotFound>> (
            UpdateAddressRequest req,
            ClaimsPrincipal claims,
            IUserRepository userRepo,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var userId = GetUserId(claims);
            if (userId is null) return TypedResults.NotFound();

            var user = await userRepo.GetByIdAsync(userId.Value, ct);
            if (user is null) return TypedResults.NotFound();

            user.UpdateAddress(req.Address, req.Note);
            await uow.SaveChangesAsync(ct);
            return TypedResults.Ok();
        });

        // ── Orders ──
        accountGroup.MapGet("/orders", async Task<Results<Ok<List<OrderSummaryDto>>, NotFound>> (
            ClaimsPrincipal claims,
            IOrderRepository orderRepo,
            CancellationToken ct) =>
        {
            var userId = GetUserId(claims);
            if (userId is null) return TypedResults.NotFound();

            var orders = await orderRepo.GetByUserIdAsync(userId.Value, ct);
            var dtos = orders.Select(o => new OrderSummaryDto(
                o.Id,
                o.Id.ToString()[..8].ToUpperInvariant(),
                o.Status,
                o.TotalAmount,
                o.Items.Count,
                o.CreatedAt)).ToList();

            return TypedResults.Ok(dtos);
        });

        accountGroup.MapPost("/orders", async Task<Results<Ok<CreateOrderResponse>, NotFound, BadRequest<string>>> (
            CreateOrderRequest req,
            ClaimsPrincipal claims,
            IOrderRepository orderRepo,
            IUnitOfWork uow,
            Nuts.Infrastructure.Services.IMoySkladService moySklad,
            CancellationToken ct) =>
        {
            var userId = GetUserId(claims);
            if (userId is null) return TypedResults.NotFound();

            var orderResult = Order.Create(userId.Value, req.ShippingAddress);
            if (orderResult.IsFailure)
                return TypedResults.BadRequest(orderResult.Error.Message);

            var order = orderResult.Value;

            foreach (var item in req.Items)
            {
                var addResult = order.AddItem(
                    Guid.NewGuid(), item.ProductName, item.Quantity, item.UnitPrice, item.Weight);
                if (addResult.IsFailure)
                    return TypedResults.BadRequest(addResult.Error.Message);
            }

            orderRepo.Add(order);
            await uow.SaveChangesAsync(ct);

            // Push order to MoySklad (best effort, don't block checkout)
            try
            {
                var msOrder = new Nuts.Infrastructure.Services.MoySkladOrder(
                    "Заказ с сайта", "", null, req.ShippingAddress, null, null, null, null,
                    req.Items.Select(i => new Nuts.Infrastructure.Services.MoySkladOrderItem(
                        i.ProductName, i.Weight, i.Quantity, i.UnitPrice)).ToList());
                await moySklad.CreateCustomerOrderAsync(msOrder, ct);
            }
            catch { /* MoySklad unavailable — order saved locally */ }

            return TypedResults.Ok(new CreateOrderResponse(order.Id));
        });

        accountGroup.MapGet("/orders/{id:guid}", async Task<Results<Ok<OrderDetailDto>, NotFound>> (
            Guid id,
            ClaimsPrincipal claims,
            IOrderRepository orderRepo,
            CancellationToken ct) =>
        {
            var userId = GetUserId(claims);
            if (userId is null) return TypedResults.NotFound();

            var order = await orderRepo.GetByIdAsync(id, ct);
            if (order is null || order.UserId != userId) return TypedResults.NotFound();

            var items = order.Items.Select(i => new OrderItemDto(
                i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.Weight, i.Subtotal)).ToList();

            return TypedResults.Ok(new OrderDetailDto(
                order.Id,
                order.Id.ToString()[..8].ToUpperInvariant(),
                order.Status,
                order.TotalAmount,
                order.ShippingAddress,
                order.CreatedAt,
                order.UpdatedAt,
                items));
        });
    }

    private static Guid? GetUserId(ClaimsPrincipal claims)
    {
        var idClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        var computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, computed);
    }

    private static string GenerateToken(User user, IConfiguration config)
    {
        var key = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? config["Jwt:Key"]
            ?? "NutsSecretKeyForDevAtLeast32Bytes!!";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, "User")
            ]),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = credentials,
            Issuer = config["Jwt:Issuer"] ?? "NutsApi",
            Audience = config["Jwt:Audience"] ?? "NutsAdmin"
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(descriptor);
    }
}

// ── DTOs ──
public sealed record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FullName { get; init; }
    public string? Phone { get; init; }
}

public sealed record UserLoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public sealed record AuthResponse(string Token, string FullName, string Email);

public sealed record ProfileResponse(
    Guid Id, string FullName, string Email, string? Phone,
    string? Address, string? AddressNote, DateTime CreatedAt);

public sealed record UpdateProfileRequest
{
    public required string FullName { get; init; }
    public string? Phone { get; init; }
}

public sealed record OrderSummaryDto(
    Guid Id, string OrderNumber, string Status, decimal TotalAmount, int ItemsCount, DateTime CreatedAt);

public sealed record OrderDetailDto(
    Guid Id, string OrderNumber, string Status, decimal TotalAmount,
    string ShippingAddress, DateTime CreatedAt, DateTime? UpdatedAt, List<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, string Weight, decimal Subtotal);

public sealed record CreateOrderRequest
{
    public required List<CreateOrderItemRequest> Items { get; init; }
    public required string ShippingAddress { get; init; }
}

public sealed record CreateOrderItemRequest
{
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required string Weight { get; init; }
}

public sealed record UpdateAddressRequest
{
    public string? Address { get; init; }
    public string? Note { get; init; }
}

public sealed record CreateOrderResponse(Guid OrderId);
