using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class OrderItem : AggregateRoot<Guid>
{
    private OrderItem() { }

    public Guid OrderId { get; private init; }
    public Guid ProductId { get; private init; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Weight { get; private set; } = string.Empty;
    public decimal Subtotal => Quantity * UnitPrice;

    public static Result<OrderItem> Create(
        Guid orderId, Guid productId, string productName,
        int quantity, decimal unitPrice, string weight)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return Result<OrderItem>.Failure(new Error("OrderItem.NameRequired", "Product name is required."));

        if (quantity <= 0)
            return Result<OrderItem>.Failure(new Error("OrderItem.InvalidQuantity", "Quantity must be positive."));

        if (unitPrice < 0)
            return Result<OrderItem>.Failure(new Error("OrderItem.InvalidPrice", "Price cannot be negative."));

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            Weight = weight.Trim()
        };
    }
}
