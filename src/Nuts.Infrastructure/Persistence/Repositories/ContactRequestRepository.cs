using Microsoft.EntityFrameworkCore;
using Nuts.Application.Contacts;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Repositories;

internal sealed class ContactRequestRepository(AppDbContext db) : IContactRequestRepository
{
    public Task<List<ContactRequest>> GetAllAsync(CancellationToken ct = default) =>
        db.ContactRequests.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

    public Task<ContactRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ContactRequests.FirstOrDefaultAsync(c => c.Id == id, ct);

    public void Add(ContactRequest request) => db.ContactRequests.Add(request);

    public void Remove(ContactRequest request) => db.ContactRequests.Remove(request);
}
