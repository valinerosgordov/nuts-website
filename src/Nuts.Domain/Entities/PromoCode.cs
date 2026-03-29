using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class PromoCode : AggregateRoot<Guid>
{
    private PromoCode() { }

    public string Code { get; private set; } = string.Empty;
    public int DiscountPercent { get; private set; }
    public decimal? MinOrderAmount { get; private set; }
    public int MaxUses { get; private set; }
    public int CurrentUses { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private init; }

    public static Result<PromoCode> Create(string code, int discountPercent, int maxUses, decimal? minOrderAmount = null, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result<PromoCode>.Failure(new Error("Promo.CodeRequired", "Code is required."));
        if (discountPercent < 1 || discountPercent > 99)
            return Result<PromoCode>.Failure(new Error("Promo.InvalidDiscount", "Discount must be 1-99%."));
        if (maxUses < 1)
            return Result<PromoCode>.Failure(new Error("Promo.InvalidMaxUses", "Max uses must be at least 1."));

        return new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            DiscountPercent = discountPercent,
            MaxUses = maxUses,
            MinOrderAmount = minOrderAmount,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result Update(string code, int discountPercent, int maxUses, decimal? minOrderAmount, DateTime? expiresAt, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure(new Error("Promo.CodeRequired", "Code is required."));
        if (discountPercent < 1 || discountPercent > 99)
            return Result.Failure(new Error("Promo.InvalidDiscount", "Discount must be 1-99%."));

        Code = code.Trim().ToUpperInvariant();
        DiscountPercent = discountPercent;
        MaxUses = maxUses;
        MinOrderAmount = minOrderAmount;
        ExpiresAt = expiresAt;
        IsActive = isActive;
        return Result.Success();
    }

    public Result<int> Apply(decimal orderAmount)
    {
        if (!IsActive)
            return Result<int>.Failure(new Error("Promo.Inactive", "Промокод неактивен."));
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
            return Result<int>.Failure(new Error("Promo.Expired", "Промокод истёк."));
        if (CurrentUses >= MaxUses)
            return Result<int>.Failure(new Error("Promo.MaxUsesReached", "Промокод больше не действует."));
        if (MinOrderAmount.HasValue && orderAmount < MinOrderAmount.Value)
            return Result<int>.Failure(new Error("Promo.MinAmount", $"Минимальная сумма заказа: {MinOrderAmount.Value} \u20BD"));

        CurrentUses++;
        return DiscountPercent;
    }
}
