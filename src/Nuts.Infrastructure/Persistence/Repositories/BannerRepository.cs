using Microsoft.EntityFrameworkCore;
using Nuts.Application.Products;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Repositories;

internal sealed class BannerRepository(AppDbContext db) : IBannerRepository
{
    public Task<Banner?> GetActiveAsync(CancellationToken ct) =>
        db.Set<Banner>().FirstOrDefaultAsync(b => b.IsActive, ct);

    public Task<Banner?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Set<Banner>().FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<List<Banner>> GetAllAsync(CancellationToken ct) =>
        db.Set<Banner>().OrderByDescending(b => b.CreatedAt).ToListAsync(ct);

    public void Add(Banner banner) => db.Set<Banner>().Add(banner);
    public void Remove(Banner banner) => db.Set<Banner>().Remove(banner);
}
