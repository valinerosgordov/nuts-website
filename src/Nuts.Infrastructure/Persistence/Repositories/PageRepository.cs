using Microsoft.EntityFrameworkCore;
using Nuts.Application.Common;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Repositories;

internal sealed class PageRepository(AppDbContext db) : IPageRepository
{
    public Task<List<Page>> GetAllAsync(CancellationToken ct = default) =>
        db.Pages.OrderBy(p => p.Slug).ToListAsync(ct);

    public Task<Page?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        db.Pages.FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<Page?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Pages.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Add(Page page) => db.Pages.Add(page);

    public void Remove(Page page) => db.Pages.Remove(page);
}
