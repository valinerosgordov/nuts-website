using Nuts.Domain.Entities;

namespace Nuts.Application.Account;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    void Add(Order order);
}
