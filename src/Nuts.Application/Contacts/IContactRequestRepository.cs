using Nuts.Domain.Entities;

namespace Nuts.Application.Contacts;

public interface IContactRequestRepository
{
    Task<List<ContactRequest>> GetAllAsync(CancellationToken ct = default);
    Task<ContactRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(ContactRequest request);
    void Remove(ContactRequest request);
}
