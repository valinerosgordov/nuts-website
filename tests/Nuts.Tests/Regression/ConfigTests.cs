using System.IO;
using System.Text.Json;

namespace Nuts.Tests.Regression;

public class ConfigTests
{
    private static readonly string ApiRoot =
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "Nuts.Api");

    // Bug: JWT key was hardcoded in appsettings.json
    [Fact]
    public void AppSettings_JwtKeyIsEmpty()
    {
        var content = File.ReadAllText(Path.Combine(ApiRoot, "appsettings.json"));
        Assert.DoesNotContain("NutsSecretKeyForDev", content);
    }

    // Bug: Admin password hash was SHA256 of "admin" in git
    [Fact]
    public void AppSettings_AdminHashIsEmpty()
    {
        var content = File.ReadAllText(Path.Combine(ApiRoot, "appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var hash = doc.RootElement.GetProperty("Admin").GetProperty("PasswordHash").GetString();
        Assert.True(string.IsNullOrEmpty(hash), "Admin:PasswordHash must be empty (use env var)");
    }

    // Bug: MoySklad token leaked in git
    [Fact]
    public void AppSettings_MoySkladTokenIsEmpty()
    {
        var content = File.ReadAllText(Path.Combine(ApiRoot, "appsettings.json"));
        using var doc = JsonDocument.Parse(content);
        var token = doc.RootElement.GetProperty("MoySklad").GetProperty("Token").GetString();
        Assert.True(string.IsNullOrEmpty(token));
    }

    // Bug: Dockerfile HEALTHCHECK used wget (not installed)
    [Fact]
    public void Dockerfile_UsesCurlForHealthcheck()
    {
        var dockerfile = Path.Combine(ApiRoot, "..", "..", "Dockerfile");
        var content = File.ReadAllText(dockerfile);
        Assert.Contains("HEALTHCHECK", content);
        Assert.Contains("curl", content);
        Assert.DoesNotContain("wget --quiet --tries=1 --spider", content);
    }

    // Bug: docker-compose was missing ConnectionStrings__Default -> DB wasn't in volume
    [Fact]
    public void DockerCompose_SetsConnectionStringToDataPath()
    {
        var composePath = Path.Combine(ApiRoot, "..", "..", "docker-compose.yml");
        var content = File.ReadAllText(composePath);
        Assert.Contains("Data Source=/app/data/nuts.db", content);
    }
}
