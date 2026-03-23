using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Nuts.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", Task<Results<Ok<LoginResponse>, UnauthorizedHttpResult>> (
            LoginRequest req, IConfiguration config) =>
        {
            var adminUser = config["Admin:Username"] ?? "admin";
            var adminPassHash = config["Admin:PasswordHash"] ?? HashPassword("admin");

            var inputHash = HashPassword(req.Password);
            var expected = Encoding.UTF8.GetBytes(adminPassHash);
            var actual = Encoding.UTF8.GetBytes(inputHash);

            if (req.Username != adminUser || !CryptographicOperations.FixedTimeEquals(actual, expected))
                return Task.FromResult<Results<Ok<LoginResponse>, UnauthorizedHttpResult>>(TypedResults.Unauthorized());

            var key = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? config["Jwt:Key"] ?? "NutsSecretKeyForDevAtLeast32Bytes!!";
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

    // TODO: Upgrade admin password hashing to PBKDF2 (currently SHA256 because the hash is stored in appsettings config)
    private static string HashPassword(string password)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexStringLower(hash);
    }
}

public sealed record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public sealed record LoginResponse(string Token);
