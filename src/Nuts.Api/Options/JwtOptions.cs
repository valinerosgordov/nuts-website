using System.ComponentModel.DataAnnotations;

namespace Nuts.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32, ErrorMessage = "JWT key must be at least 32 bytes.")]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = "NutsApi";

    [Required]
    public string Audience { get; init; } = "NutsAdmin";

    public TimeSpan AdminTokenLifetime { get; init; } = TimeSpan.FromHours(12);
    public TimeSpan UserTokenLifetime  { get; init; } = TimeSpan.FromDays(7);
}
