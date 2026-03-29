using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class ProductVariant : Entity<Guid>
{
    private ProductVariant() { }

    public Guid ProductId { get; private init; }
    public string Weight { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int SortOrder { get; private set; }

    public static Result<ProductVariant> Create(Guid productId, string weight, decimal price, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(weight))
            return Result<ProductVariant>.Failure(new Error("Variant.WeightRequired", "Weight is required."));
        if (price < 0)
            return Result<ProductVariant>.Failure(new Error("Variant.InvalidPrice", "Price cannot be negative."));

        return new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Weight = weight.Trim(),
            Price = price,
            SortOrder = sortOrder
        };
    }

    public void Update(string weight, decimal price, int sortOrder)
    {
        Weight = weight.Trim();
        Price = price;
        SortOrder = sortOrder;
    }
}
