using System.ComponentModel.DataAnnotations;

namespace Nuts.Api.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    [Required]
    public string Username { get; init; } = "admin";

    /// <summary>
    /// PBKDF2 hash in format "base64(salt):base64(hash)".
    /// Generate with: dotnet run --project src/Nuts.Api -- hash-password "password"
    /// </summary>
    [Required]
    [RegularExpression(@"^[A-Za-z0-9+/=]+:[A-Za-z0-9+/=]+$",
        ErrorMessage = "Admin password hash must be in 'base64(salt):base64(hash)' format.")]
    public string PasswordHash { get; init; } = string.Empty;
}
