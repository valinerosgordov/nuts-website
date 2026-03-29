using Microsoft.EntityFrameworkCore;
using Nuts.Application.Products;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Repositories;

internal sealed class PromoCodeRepository(AppDbContext db) : IPromoCodeRepository
{
    public Task<PromoCode?> GetByCodeAsync(string code, CancellationToken ct) =>
        db.Set<PromoCode>().FirstOrDefaultAsync(p => p.Code == code, ct);

    public Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Set<PromoCode>().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<PromoCode>> GetAllAsync(CancellationToken ct) =>
        db.Set<PromoCode>().OrderByDescending(p => p.CreatedAt).ToListAsync(ct);

    public void Add(PromoCode promoCode) => db.Set<PromoCode>().Add(promoCode);
    public void Remove(PromoCode promoCode) => db.Set<PromoCode>().Remove(promoCode);
}
