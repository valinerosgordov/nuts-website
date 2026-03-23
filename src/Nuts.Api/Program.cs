using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nuts.Api.Endpoints;
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

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
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

// Fallback to index.html for SPA-like routing
app.MapFallbackToFile("index.html");

app.Run();
