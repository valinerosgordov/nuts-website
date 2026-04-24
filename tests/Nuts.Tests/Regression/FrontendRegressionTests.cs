using System.IO;

namespace Nuts.Tests.Regression;

public class FrontendRegressionTests
{
    private static readonly string WwwRoot =
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "Nuts.Api", "wwwroot");

    // Bug: Dmitry asked to remove glow — must not return
    [Fact]
    public void Catalog_HasNoGlowCardClass()
    {
        var content = File.ReadAllText(Path.Combine(WwwRoot, "catalog.html"));
        Assert.DoesNotContain("glow-card", content);
    }

    // Bug: catalog was hardcoded with 1200+ product cards
    [Fact]
    public void Catalog_IsDynamic_NotHardcoded()
    {
        var content = File.ReadAllText(Path.Combine(WwwRoot, "catalog.html"));
        // Dynamic catalog fetches from API
        Assert.Contains("/api/products", content);
        // Should not have dozens of hardcoded catalog__item divs
        var count = System.Text.RegularExpressions.Regex.Matches(content, "class=\"catalog__item\"").Count;
        Assert.True(count < 3, $"Expected < 3 hardcoded catalog items, found {count}");
    }

    // Bug: default tabs must always include these 3
    [Fact]
    public void Catalog_HasDefaultCategoryTabs()
    {
        var content = File.ReadAllText(Path.Combine(WwwRoot, "catalog.html"));
        Assert.Contains("Орехи", content);
        Assert.Contains("Сухофрукты", content);
        Assert.Contains("Подарочные наборы", content);
    }

    // Bug: particles canvas was dead code
    [Fact]
    public void NoParticlesCanvas_InHtml()
    {
        foreach (var file in Directory.GetFiles(WwwRoot, "*.html"))
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("<canvas id=\"particles\"", content);
        }
    }

    // Bug: pointer-events on SVG was removed, mobile cart broke
    [Fact]
    public void CatalogCartButton_HasPointerEventsNoneOnSvg()
    {
        var content = File.ReadAllText(Path.Combine(WwwRoot, "catalog.html"));
        Assert.Contains("pointer-events: none", content);
    }

    // Bug: escapeHtml was defined twice
    [Fact]
    public void MainJs_EscapeHtmlDefinedOnce()
    {
        var content = File.ReadAllText(Path.Combine(WwwRoot, "js", "main.js"));
        var count = System.Text.RegularExpressions.Regex.Matches(content, @"escapeHtml\s*=\s*\(").Count;
        Assert.True(count <= 1, $"escapeHtml should be defined at most once, found {count}");
    }

    // Bug: healthcheck path mismatch between Dockerfile and Program.cs
    [Fact]
    public void Program_MapsHealthLiveEndpoint()
    {
        var programPath = Path.Combine(WwwRoot, "..", "Program.cs");
        var content = File.ReadAllText(programPath);
        Assert.Contains("/health/live", content);
    }
}
