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
    public string? CustomerName { get; private set; }
    public string? CustomerPhone { get; private set; }
    public string? CustomerEmail { get; private set; }
    public string? DeliveryTime { get; private set; }
    public string? Comment { get; private set; }
    public string? PromoCode { get; private set; }
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

    public static Result<Order> CreateGuest(
        string name, string phone, string? email,
        string address, string? deliveryTime, string? comment, string? promoCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Order>.Failure(new Error("Order.NameRequired", "Customer name is required."));

        if (string.IsNullOrWhiteSpace(phone))
            return Result<Order>.Failure(new Error("Order.PhoneRequired", "Customer phone is required."));

        if (string.IsNullOrWhiteSpace(address))
            return Result<Order>.Failure(new Error("Order.AddressRequired", "Shipping address is required."));

        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty,
            ShippingAddress = address.Trim(),
            CustomerName = name.Trim(),
            CustomerPhone = phone.Trim(),
            CustomerEmail = email?.Trim(),
            DeliveryTime = deliveryTime?.Trim(),
            Comment = comment?.Trim(),
            PromoCode = promoCode?.Trim(),
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
