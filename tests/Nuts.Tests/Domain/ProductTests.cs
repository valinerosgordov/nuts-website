using Nuts.Domain.Entities;

namespace Nuts.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Product.Create("Walnuts", "Premium walnuts", 990m, "Chile", "Nuts");

        Assert.True(result.IsSuccess);
        Assert.Equal(990m, result.Value.Price);
        Assert.True(result.Value.IsAvailable);
    }

    [Fact]
    public void Create_WithEmptyName_Fails()
    {
        var result = Product.Create("", "desc", 100m, "origin", "cat");

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NameRequired", result.Error.Code);
    }

    [Fact]
    public void Create_WithNegativePrice_Fails()
    {
        var result = Product.Create("name", "desc", -1m, "origin", "cat");

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidPrice", result.Error.Code);
    }

    [Fact]
    public void AddVariant_WithValidData_Succeeds()
    {
        var product = Product.Create("name", "desc", 100m, "origin", "cat").Value;
        var result = product.AddVariant("500", 270m);

        Assert.True(result.IsSuccess);
        Assert.Single(product.Variants);
    }

    [Fact]
    public void AddVariant_WithEmptyWeight_Fails()
    {
        var product = Product.Create("name", "desc", 100m, "origin", "cat").Value;
        var result = product.AddVariant("", 270m);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ClearVariants_RemovesAllVariants()
    {
        var product = Product.Create("name", "desc", 100m, "origin", "cat").Value;
        product.AddVariant("500g", 270m);
        product.AddVariant("1kg", 500m);

        product.ClearVariants();

        Assert.Empty(product.Variants);
    }
}
