using System.Collections.Frozen;
using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public Guid UserId { get; private init; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public string Status { get; private set; } = "Created";
    public decimal TotalAmount { get; private set; }
    public string ShippingAddress { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    private static readonly FrozenDictionary<string, FrozenSet<string>> AllowedTransitions =
        new Dictionary<string, FrozenSet<string>>
        {
            ["Created"] = new[] { "Processing", "Cancelled" }.ToFrozenSet(),
            ["Processing"] = new[] { "Shipped", "Cancelled" }.ToFrozenSet(),
            ["Shipped"] = new[] { "Delivered" }.ToFrozenSet(),
            ["Delivered"] = FrozenSet<string>.Empty,
            ["Cancelled"] = FrozenSet<string>.Empty,
        }.ToFrozenDictionary();

    public static Result<Order> Create(Guid userId, string shippingAddress)
    {
        if (userId == Guid.Empty)
            return Result<Order>.Failure(new Error("Order.UserRequired", "User is required."));

        if (string.IsNullOrWhiteSpace(shippingAddress))
            return Result<Order>.Failure(new Error("Order.AddressRequired", "Shipping address is required."));

        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShippingAddress = shippingAddress.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result AddItem(Guid productId, string productName, int quantity, decimal unitPrice, string weight)
    {
        var itemResult = OrderItem.Create(Id, productId, productName, quantity, unitPrice, weight);
        if (itemResult.IsFailure)
            return Result.Failure(itemResult.Error);

        _items.Add(itemResult.Value);
        CalculateTotal();
        return Result.Success();
    }

    public Result UpdateStatus(string newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            return Result.Failure(new Error("Order.InvalidTransition",
                $"Cannot transition from '{Status}' to '{newStatus}'."));

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    private void CalculateTotal() =>
        TotalAmount = _items.Sum(i => i.Subtotal);
}
