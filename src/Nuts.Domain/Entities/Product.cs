using System.Net;
using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class Product : AggregateRoot<Guid>
{
    private Product() { }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ImagePath { get; private set; }
    public decimal Price { get; private set; }
    public string Origin { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsAvailable { get; private set; } = true;
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    public static Result<Product> Create(
        string name,
        string description,
        decimal price,
        string origin,
        string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Product>.Failure(new Error("Product.NameRequired", "Name is required."));

        if (price < 0)
            return Result<Product>.Failure(new Error("Product.InvalidPrice", "Price cannot be negative."));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = WebUtility.HtmlEncode(name.Trim()),
            Description = WebUtility.HtmlEncode(description.Trim()),
            Price = price,
            Origin = WebUtility.HtmlEncode(origin.Trim()),
            Category = WebUtility.HtmlEncode(category.Trim()),
            CreatedAt = DateTime.UtcNow
        };

        return product;
    }

    public Result Update(string name, string description, decimal price, string origin, string category, bool isAvailable, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("Product.NameRequired", "Name is required."));

        if (price < 0)
            return Result.Failure(new Error("Product.InvalidPrice", "Price cannot be negative."));

        Name = WebUtility.HtmlEncode(name.Trim());
        Description = WebUtility.HtmlEncode(description.Trim());
        Price = price;
        Origin = WebUtility.HtmlEncode(origin.Trim());
        Category = WebUtility.HtmlEncode(category.Trim());
        IsAvailable = isAvailable;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public void SetImage(string? imagePath) => ImagePath = imagePath;
}
