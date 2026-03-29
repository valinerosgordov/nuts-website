using Microsoft.AspNetCore.Http.HttpResults;
using Nuts.Application.Common;
using Nuts.Application.Products;
using Nuts.Domain.Entities;

namespace Nuts.Api.Endpoints;

public static class BannerEndpoints
{
    public static void MapBannerEndpoints(this IEndpointRouteBuilder app)
    {
        // Public — get active banner
        app.MapGet("/api/banners/active", async Task<Results<Ok<BannerDto>, NoContent>> (
            IBannerRepository repo, CancellationToken ct) =>
        {
            var banner = await repo.GetActiveAsync(ct);
            if (banner is null) return TypedResults.NoContent();
            return TypedResults.Ok(new BannerDto(
                banner.Id, banner.Title, banner.Description,
                banner.ButtonText, banner.ButtonUrl, banner.ImageUrl,
                banner.DelaySeconds, banner.IsActive, banner.CreatedAt));
        }).WithTags("Banners");

        // Admin CRUD
        var adminGroup = app.MapGroup("/api/admin/banners")
            .WithTags("Admin — Banners")
            .RequireAuthorization("Admin");

        adminGroup.MapGet("/", async (IBannerRepository repo, CancellationToken ct) =>
        {
            var banners = await repo.GetAllAsync(ct);
            return TypedResults.Ok(banners.Select(b => new BannerDto(
                b.Id, b.Title, b.Description,
                b.ButtonText, b.ButtonUrl, b.ImageUrl,
                b.DelaySeconds, b.IsActive, b.CreatedAt)));
        });

        adminGroup.MapPost("/", async Task<Results<Created<BannerDto>, BadRequest<ProblemHttpResult>>> (
            CreateBannerRequest req, IBannerRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var result = Banner.Create(req.Title, req.Description, req.ButtonText, req.ButtonUrl, req.DelaySeconds);
            return await result.Match<Task<Results<Created<BannerDto>, BadRequest<ProblemHttpResult>>>>(
                async banner =>
                {
                    repo.Add(banner);
                    await uow.SaveChangesAsync(ct);
                    var dto = new BannerDto(banner.Id, banner.Title, banner.Description,
                        banner.ButtonText, banner.ButtonUrl, banner.ImageUrl,
                        banner.DelaySeconds, banner.IsActive, banner.CreatedAt);
                    return TypedResults.Created($"/api/admin/banners/{banner.Id}", dto);
                },
                error => Task.FromResult<Results<Created<BannerDto>, BadRequest<ProblemHttpResult>>>(
                    TypedResults.BadRequest(TypedResults.Problem(error.Message, statusCode: 400))));
        });

        adminGroup.MapPut("/{id:guid}", async Task<Results<Ok<BannerDto>, NotFound, BadRequest<ProblemHttpResult>>> (
            Guid id, UpdateBannerRequest req, IBannerRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var banner = await repo.GetByIdAsync(id, ct);
            if (banner is null) return TypedResults.NotFound();

            var updateResult = banner.Update(req.Title, req.Description, req.ButtonText, req.ButtonUrl, req.DelaySeconds, req.IsActive);
            if (updateResult.IsFailure)
                return TypedResults.BadRequest(TypedResults.Problem(updateResult.Error.Message, statusCode: 400));

            await uow.SaveChangesAsync(ct);
            var dto = new BannerDto(banner.Id, banner.Title, banner.Description,
                banner.ButtonText, banner.ButtonUrl, banner.ImageUrl,
                banner.DelaySeconds, banner.IsActive, banner.CreatedAt);
            return TypedResults.Ok(dto);
        });

        adminGroup.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id, IBannerRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var banner = await repo.GetByIdAsync(id, ct);
            if (banner is null) return TypedResults.NotFound();
            repo.Remove(banner);
            await uow.SaveChangesAsync(ct);
            return TypedResults.NoContent();
        });
    }
}

public sealed record BannerDto(
    Guid Id, string Title, string Description,
    string? ButtonText, string? ButtonUrl, string? ImageUrl,
    int DelaySeconds, bool IsActive, DateTime CreatedAt);

public sealed record CreateBannerRequest
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? ButtonText { get; init; }
    public string? ButtonUrl { get; init; }
    public int DelaySeconds { get; init; } = 3;
}

public sealed record UpdateBannerRequest
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? ButtonText { get; init; }
    public string? ButtonUrl { get; init; }
    public int DelaySeconds { get; init; } = 3;
    public required bool IsActive { get; init; }
}
