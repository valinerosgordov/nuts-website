using Nuts.Domain.Entities;

namespace Nuts.Application.Account;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    void Add(User user);
}
