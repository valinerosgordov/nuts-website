using Nuts.Domain.Entities;

namespace Nuts.Application.Products;

public interface IBannerRepository
{
    Task<Banner?> GetActiveAsync(CancellationToken ct = default);
    Task<Banner?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Banner>> GetAllAsync(CancellationToken ct = default);
    void Add(Banner banner);
    void Remove(Banner banner);
}
