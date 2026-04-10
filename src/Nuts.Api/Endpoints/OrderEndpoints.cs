using Microsoft.AspNetCore.Http.HttpResults;
using Nuts.Application.Account;
using Nuts.Application.Common;
using Nuts.Domain.Entities;
using Nuts.Infrastructure.Services;

namespace Nuts.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup("/api/orders").WithTags("Orders — Public");
        var adminGroup = app.MapGroup("/api/admin/orders").WithTags("Orders — Admin").RequireAuthorization("Admin");

        // ── Public: create guest order (no auth) ──
        publicGroup.MapPost("/", async Task<Results<Ok<PublicOrderResponse>, BadRequest<string>>> (
            PublicCreateOrderRequest req,
            IOrderRepository orderRepo,
            IUnitOfWork uow,
            IMoySkladService moySklad,
            CancellationToken ct) =>
        {
            var orderResult = Order.CreateGuest(
                req.Name, req.Phone, req.Email,
                req.Address, req.DeliveryTime, req.Comment, req.PromoCode);

            if (orderResult.IsFailure)
                return TypedResults.BadRequest(orderResult.Error.Message);

            var order = orderResult.Value;

            foreach (var item in req.Items)
            {
                var addResult = order.AddItem(
                    Guid.NewGuid(), item.ProductName, item.Quantity, item.UnitPrice, item.Weight);
                if (addResult.IsFailure)
                    return TypedResults.BadRequest(addResult.Error.Message);
            }

            orderRepo.Add(order);
            await uow.SaveChangesAsync(ct);

            // Push to MoySklad (best effort)
            try
            {
                var msOrder = new MoySkladOrder(
                    req.Name, req.Phone, req.Email,
                    req.Address, req.DeliveryTime, req.Comment, null, req.PromoCode,
                    req.Items.Select(i => new MoySkladOrderItem(
                        i.ProductName, i.Weight, i.Quantity, i.UnitPrice)).ToList());
                await moySklad.CreateCustomerOrderAsync(msOrder, ct);
            }
            catch { /* MoySklad unavailable — order saved locally */ }

            return TypedResults.Ok(new PublicOrderResponse(
                order.Id,
                order.Id.ToString()[..8].ToUpperInvariant()));
        }).AllowAnonymous();

        // ── Admin: list all orders ──
        adminGroup.MapGet("/", async Task<Ok<List<AdminOrderDto>>> (
            IOrderRepository orderRepo,
            CancellationToken ct) =>
        {
            var orders = await orderRepo.GetAllAsync(ct);
            var dtos = orders.Select(MapToAdminDto).ToList();
            return TypedResults.Ok(dtos);
        });

        // ── Admin: get single order ──
        adminGroup.MapGet("/{id:guid}", async Task<Results<Ok<AdminOrderDto>, NotFound>> (
            Guid id,
            IOrderRepository orderRepo,
            CancellationToken ct) =>
        {
            var order = await orderRepo.GetByIdAsync(id, ct);
            if (order is null) return TypedResults.NotFound();
            return TypedResults.Ok(MapToAdminDto(order));
        });

        // ── Admin: update order status ──
        adminGroup.MapPut("/{id:guid}/status", async Task<Results<Ok<AdminOrderDto>, NotFound, BadRequest<string>>> (
            Guid id,
            UpdateOrderStatusRequest req,
            IOrderRepository orderRepo,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var order = await orderRepo.GetByIdAsync(id, ct);
            if (order is null) return TypedResults.NotFound();

            var result = order.UpdateStatus(req.Status);
            if (result.IsFailure)
                return TypedResults.BadRequest(result.Error.Message);

            await uow.SaveChangesAsync(ct);
            return TypedResults.Ok(MapToAdminDto(order));
        });
    }

    private static AdminOrderDto MapToAdminDto(Order order) => new(
        order.Id,
        order.Id.ToString()[..8].ToUpperInvariant(),
        order.CustomerName ?? "Зарегистрированный",
        order.CustomerPhone,
        order.CustomerEmail,
        order.Status,
        order.TotalAmount,
        order.Items.Count,
        order.Items.Select(i => new AdminOrderItemDto(
            i.Id, i.ProductName, i.Weight, i.Quantity, i.UnitPrice, i.Subtotal)).ToList(),
        order.ShippingAddress,
        order.DeliveryTime,
        order.Comment,
        order.PromoCode,
        order.CreatedAt,
        order.UpdatedAt);
}

// ── DTOs ──
public sealed record PublicCreateOrderRequest
{
    public required string Name { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public required string Address { get; init; }
    public string? DeliveryTime { get; init; }
    public string? Comment { get; init; }
    public string? PromoCode { get; init; }
    public required List<PublicOrderItemRequest> Items { get; init; }
}

public sealed record PublicOrderItemRequest
{
    public required string ProductName { get; init; }
    public required string Weight { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

public sealed record PublicOrderResponse(Guid OrderId, string OrderNumber);

public sealed record AdminOrderDto(
    Guid Id,
    string OrderNumber,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    string Status,
    decimal TotalAmount,
    int ItemCount,
    List<AdminOrderItemDto> Items,
    string ShippingAddress,
    string? DeliveryTime,
    string? Comment,
    string? PromoCode,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminOrderItemDto(
    Guid Id, string ProductName, string Weight, int Quantity, decimal UnitPrice, decimal Subtotal);

public sealed record UpdateOrderStatusRequest
{
    public required string Status { get; init; }
}
