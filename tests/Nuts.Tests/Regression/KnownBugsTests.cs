using Nuts.Domain.Entities;

namespace Nuts.Tests.Regression;

public class KnownBugsTests
{
    // Bug: variant update used Guid.NewGuid() instead of preserving ProductId
    [Fact]
    public void Order_AddItem_PreservesProductId()
    {
        var productId = Guid.NewGuid();
        var order = Order.CreateGuest("Test", "+70000000000", null, "addr", null, null, null).Value;
        order.AddItem(productId, "Nuts", 1, 100m, "500г");

        Assert.Equal(productId, order.Items[0].ProductId);
    }

    // Bug: State machine allowed Created → Delivered (skipping Processing/Shipped)
    [Fact]
    public void Order_CannotJumpFromCreatedToDelivered()
    {
        var order = Order.Create(Guid.NewGuid(), "addr").Value;
        var result = order.UpdateStatus("Delivered");
        Assert.True(result.IsFailure);
    }

    // Bug: Delivered was treated as non-terminal (could go back to Processing)
    [Fact]
    public void Order_DeliveredIsTerminal()
    {
        var order = Order.Create(Guid.NewGuid(), "addr").Value;
        order.UpdateStatus("Processing");
        order.UpdateStatus("Shipped");
        order.UpdateStatus("Delivered");
        var result = order.UpdateStatus("Processing");
        Assert.True(result.IsFailure);
    }

    // Bug: HtmlEncode was missing on Order.CreateGuest fields (XSS risk)
    [Fact]
    public void Order_CreateGuest_EncodesHtmlInName()
    {
        var order = Order.CreateGuest("<script>alert(1)</script>", "+70000000000", null, "addr", null, null, null).Value;
        Assert.DoesNotContain("<script>", order.CustomerName);
        Assert.Contains("&lt;", order.CustomerName);
    }

    // Bug: Product.Create didn't HtmlEncode name
    [Fact]
    public void Product_Create_EncodesHtmlInName()
    {
        var p = Product.Create("<img onerror>", "desc", 100m, "origin", "cat").Value;
        Assert.DoesNotContain("<img", p.Name);
    }

    // Bug: ProductVariant.Create accepted empty weight
    [Fact]
    public void ProductVariant_RejectsEmptyWeight()
    {
        var result = ProductVariant.Create(Guid.NewGuid(), "", 100m);
        Assert.True(result.IsFailure);
    }

    // Bug: PromoCode accepted discount > 99
    [Fact]
    public void PromoCode_RejectsDiscountOver99()
    {
        var result = PromoCode.Create("X", 100, 10);
        Assert.True(result.IsFailure);
    }

    // Bug: PromoCode.Apply didn't check expiration
    [Fact]
    public void PromoCode_ExpiredCodeCannotApply()
    {
        var promo = PromoCode.Create("X", 10, 100, expiresAt: DateTime.UtcNow.AddSeconds(-1)).Value;
        var result = promo.Apply(10000m);
        Assert.True(result.IsFailure);
    }

    // Bug: Banner.Create didn't HtmlEncode title
    [Fact]
    public void Banner_Create_EncodesHtml()
    {
        var b = Banner.Create("<script>", "desc").Value;
        Assert.DoesNotContain("<script>", b.Title);
    }
}
