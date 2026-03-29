using Nuts.Domain.Entities;

namespace Nuts.Application.Products;

public interface IPromoCodeRepository
{
    Task<PromoCode?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PromoCode>> GetAllAsync(CancellationToken ct = default);
    void Add(PromoCode promoCode);
    void Remove(PromoCode promoCode);
}
