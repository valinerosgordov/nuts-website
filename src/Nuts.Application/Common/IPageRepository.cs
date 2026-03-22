using Nuts.Domain.Entities;

namespace Nuts.Application.Common;

public interface IPageRepository
{
    Task<List<Page>> GetAllAsync(CancellationToken ct = default);
    Task<Page?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Page?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(Page page);
    void Remove(Page page);
}
