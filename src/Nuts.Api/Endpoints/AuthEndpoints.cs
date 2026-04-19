using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Nuts.Api.Security;

namespace Nuts.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", Task<Results<Ok<LoginResponse>, UnauthorizedHttpResult>> (
            LoginRequest req, IConfiguration config) =>
        {
            var adminUser = config["Admin:Username"] ?? "admin";
            var adminPassHash = config["Admin:PasswordHash"] ?? string.Empty;

            // Reject if admin hash is not configured — never allow a default password.
            if (string.IsNullOrWhiteSpace(adminPassHash)
                || req.Username != adminUser
                || !PasswordHasher.Verify(req.Password, adminPassHash))
            {
                return Task.FromResult<Results<Ok<LoginResponse>, UnauthorizedHttpResult>>(
                    TypedResults.Unauthorized());
            }

            var key = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? config["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT_SECRET_KEY env var or Jwt:Key config required");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, adminUser),
                    new Claim(ClaimTypes.Role, "Admin")
                ]),
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials = credentials,
                Issuer = config["Jwt:Issuer"] ?? "NutsApi",
                Audience = config["Jwt:Audience"] ?? "NutsAdmin"
            };

            var handler = new JsonWebTokenHandler();
            var token = handler.CreateToken(descriptor);

            return Task.FromResult<Results<Ok<LoginResponse>, UnauthorizedHttpResult>>(
                TypedResults.Ok(new LoginResponse(token)));
        }).WithTags("Auth").AllowAnonymous();
    }
}

public sealed record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public sealed record LoginResponse(string Token);
