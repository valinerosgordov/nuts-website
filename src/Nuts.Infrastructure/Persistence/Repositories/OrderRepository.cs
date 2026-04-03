using Microsoft.EntityFrameworkCore;
using Nuts.Application.Account;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Orders.Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Order>> GetAllAsync(CancellationToken ct = default) =>
        db.Orders.Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public void Add(Order order) => db.Orders.Add(order);
}
