using Nuts.Domain.Entities;

namespace Nuts.Tests.Domain;

public class PromoCodeTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = PromoCode.Create("SALE10", 10, 100);

        Assert.True(result.IsSuccess);
        Assert.Equal("SALE10", result.Value.Code);
    }

    [Fact]
    public void Create_DiscountOver99_Fails()
    {
        var result = PromoCode.Create("X", 100, 10);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_DiscountUnder1_Fails()
    {
        var result = PromoCode.Create("X", 0, 10);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Apply_IncrementsCurrentUses()
    {
        var promo = PromoCode.Create("X", 10, 5).Value;
        promo.Apply(1000m);
        Assert.Equal(1, promo.CurrentUses);
    }

    [Fact]
    public void Apply_WhenMaxUsesReached_Fails()
    {
        var promo = PromoCode.Create("X", 10, 1).Value;
        promo.Apply(1000m);
        var result = promo.Apply(1000m);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Apply_WhenMinAmountNotMet_Fails()
    {
        var promo = PromoCode.Create("X", 10, 100, minOrderAmount: 5000m).Value;
        var result = promo.Apply(1000m);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Apply_WhenExpired_Fails()
    {
        var promo = PromoCode.Create("X", 10, 100, expiresAt: DateTime.UtcNow.AddDays(-1)).Value;
        var result = promo.Apply(1000m);
        Assert.True(result.IsFailure);
    }
}
