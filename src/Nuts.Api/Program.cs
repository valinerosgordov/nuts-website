using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nuts.Api.Endpoints;
using Nuts.Domain.Entities;
using Nuts.Infrastructure;
using Nuts.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (EF Core + repos)
builder.Services.AddInfrastructure(builder.Configuration);

// Auth
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? "NutsSecretKeyForDevAtLeast32Bytes!!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NutsApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NutsAdmin",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
    .AddPolicy("User", policy => policy.RequireRole("User"));

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

// Auto-migrate & seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Seed products from JSON if DB is empty
    if (!await db.Products.AnyAsync())
    {
        var jsonPath = Path.Combine(app.Environment.ContentRootPath, "products_catalog.json");
        if (!File.Exists(jsonPath))
            jsonPath = Path.Combine(app.Environment.ContentRootPath, "..", "..", "products_catalog.json");
        if (File.Exists(jsonPath))
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            var catalog = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (catalog is not null)
            {
                var categoryMap = new Dictionary<string, string>
                {
                    ["Отборные орехи"] = "Орехи",
                    ["Сухофрукты"] = "Сухофрукты",
                    ["Подарочные наборы"] = "Подарочные наборы",
                };

                var sortOrder = 0;
                foreach (var (categoryName, data) in catalog)
                {
                    if (!categoryMap.ContainsKey(categoryName))
                        continue;

                    var displayCategory = categoryMap[categoryName];
                    var products = data.GetProperty("products");

                    foreach (var p in products.EnumerateArray())
                    {
                        var name = p.GetProperty("name").GetString() ?? "";
                        var origin = p.TryGetProperty("description_short", out var ds) ? ds.GetString() ?? "" : "";
                        var imageUrl = p.TryGetProperty("image_url", out var img) ? img.GetString() : null;

                        // Get first variant price
                        decimal price = 0;
                        if (p.TryGetProperty("variants", out var variants))
                        {
                            var first = variants.EnumerateArray().FirstOrDefault();
                            if (first.TryGetProperty("price", out var priceEl))
                                decimal.TryParse(priceEl.GetString(), out price);
                        }

                        var result = Product.Create(name, name, price, origin, displayCategory);
                        if (result.IsSuccess)
                        {
                            var product = result.Value;
                            product.SetImage(imageUrl);

                            if (p.TryGetProperty("variants", out var seedVariants))
                            {
                                var variantOrder = 0;
                                foreach (var v in seedVariants.EnumerateArray())
                                {
                                    var option = v.TryGetProperty("option", out var opt) ? opt.GetString() ?? "" : "";
                                    var variantPrice = 0m;
                                    if (v.TryGetProperty("price", out var vp))
                                        decimal.TryParse(vp.GetString(), out variantPrice);
                                    if (!string.IsNullOrEmpty(option) && variantPrice > 0)
                                        product.AddVariant(option, variantPrice, variantOrder++);
                                }
                            }

                            db.Products.Add(product);
                        }
                    }
                }
                await db.SaveChangesAsync();
            }
        }
    }
}

app.UseCors();

// Static files (frontend + admin) — must be before auth middleware
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// API Endpoints
app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapContactEndpoints();
app.MapMediaEndpoints();
app.MapAccountEndpoints();
app.MapPromoCodeEndpoints();
app.MapSettingsEndpoints();
app.MapBannerEndpoints();
app.MapMoySkladEndpoints();
app.MapOrderEndpoints();

// Fallback to index.html for SPA-like routing
app.MapFallbackToFile("index.html");

app.Run();
