using Microsoft.AspNetCore.Http.HttpResults;
using Nuts.Application.Common;
using Nuts.Application.Products;
using Nuts.Domain.Entities;

namespace Nuts.Api.Endpoints;

public static class PromoCodeEndpoints
{
    public static void MapPromoCodeEndpoints(this IEndpointRouteBuilder app)
    {
        // Public — validate promo code (does NOT increment usage)
        app.MapPost("/api/promo/validate", async Task<Results<Ok<PromoValidateResponse>, BadRequest<PromoValidateResponse>>> (
            PromoValidateRequest req, IPromoCodeRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Code))
                return TypedResults.BadRequest(new PromoValidateResponse(false, 0, "Введите промокод."));

            var promo = await repo.GetByCodeAsync(req.Code.Trim().ToUpperInvariant(), ct);
            if (promo is null)
                return TypedResults.BadRequest(new PromoValidateResponse(false, 0, "Промокод не найден."));

            if (!promo.IsActive)
                return TypedResults.BadRequest(new PromoValidateResponse(false, 0, "Промокод неактивен."));
            if (promo.ExpiresAt.HasValue && promo.ExpiresAt.Value < DateTime.UtcNow)
                return TypedResults.BadRequest(new PromoValidateResponse(false, 0, "Промокод истёк."));
            if (promo.CurrentUses >= promo.MaxUses)
                return TypedResults.BadRequest(new PromoValidateResponse(false, 0, "Промокод больше не действует."));
            if (promo.MinOrderAmount.HasValue && req.OrderAmount < promo.MinOrderAmount.Value)
                return TypedResults.BadRequest(new PromoValidateResponse(false, 0, $"Минимальная сумма заказа: {promo.MinOrderAmount.Value} \u20BD"));

            return TypedResults.Ok(new PromoValidateResponse(true, promo.DiscountPercent, $"Скидка {promo.DiscountPercent}% применена!"));
        }).WithTags("Promo");

        // Admin endpoints
        var adminGroup = app.MapGroup("/api/admin/promo")
            .WithTags("Admin — Promo")
            .RequireAuthorization("Admin");

        adminGroup.MapGet("/", async (IPromoCodeRepository repo, CancellationToken ct) =>
        {
            var promos = await repo.GetAllAsync(ct);
            return TypedResults.Ok(promos.Select(p => new PromoCodeDto(
                p.Id, p.Code, p.DiscountPercent, p.MinOrderAmount,
                p.MaxUses, p.CurrentUses, p.IsActive, p.ExpiresAt, p.CreatedAt)));
        });

        adminGroup.MapPost("/", async Task<Results<Created<PromoCodeDto>, BadRequest<ProblemHttpResult>>> (
            CreatePromoRequest req, IPromoCodeRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var result = PromoCode.Create(req.Code, req.DiscountPercent, req.MaxUses, req.MinOrderAmount, req.ExpiresAt);
            return await result.Match<Task<Results<Created<PromoCodeDto>, BadRequest<ProblemHttpResult>>>>(
                async promo =>
                {
                    repo.Add(promo);
                    await uow.SaveChangesAsync(ct);
                    var dto = new PromoCodeDto(promo.Id, promo.Code, promo.DiscountPercent, promo.MinOrderAmount,
                        promo.MaxUses, promo.CurrentUses, promo.IsActive, promo.ExpiresAt, promo.CreatedAt);
                    return TypedResults.Created($"/api/admin/promo/{promo.Id}", dto);
                },
                error => Task.FromResult<Results<Created<PromoCodeDto>, BadRequest<ProblemHttpResult>>>(
                    TypedResults.BadRequest(TypedResults.Problem(error.Message, statusCode: 400))));
        });

        adminGroup.MapPut("/{id:guid}", async Task<Results<Ok<PromoCodeDto>, NotFound, BadRequest<ProblemHttpResult>>> (
            Guid id, UpdatePromoRequest req, IPromoCodeRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var promo = await repo.GetByIdAsync(id, ct);
            if (promo is null) return TypedResults.NotFound();

            var updateResult = promo.Update(req.Code, req.DiscountPercent, req.MaxUses, req.MinOrderAmount, req.ExpiresAt, req.IsActive);
            if (updateResult.IsFailure)
                return TypedResults.BadRequest(TypedResults.Problem(updateResult.Error.Message, statusCode: 400));

            await uow.SaveChangesAsync(ct);
            var dto = new PromoCodeDto(promo.Id, promo.Code, promo.DiscountPercent, promo.MinOrderAmount,
                promo.MaxUses, promo.CurrentUses, promo.IsActive, promo.ExpiresAt, promo.CreatedAt);
            return TypedResults.Ok(dto);
        });

        adminGroup.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id, IPromoCodeRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var promo = await repo.GetByIdAsync(id, ct);
            if (promo is null) return TypedResults.NotFound();
            repo.Remove(promo);
            await uow.SaveChangesAsync(ct);
            return TypedResults.NoContent();
        });
    }
}

public sealed record PromoValidateRequest
{
    public required string Code { get; init; }
    public required decimal OrderAmount { get; init; }
}

public sealed record PromoValidateResponse(bool Valid, int DiscountPercent, string Message);

public sealed record PromoCodeDto(
    Guid Id, string Code, int DiscountPercent, decimal? MinOrderAmount,
    int MaxUses, int CurrentUses, bool IsActive, DateTime? ExpiresAt, DateTime CreatedAt);

public sealed record CreatePromoRequest
{
    public required string Code { get; init; }
    public required int DiscountPercent { get; init; }
    public required int MaxUses { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public sealed record UpdatePromoRequest
{
    public required string Code { get; init; }
    public required int DiscountPercent { get; init; }
    public required int MaxUses { get; init; }
    public required bool IsActive { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
