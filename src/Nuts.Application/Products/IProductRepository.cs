using Nuts.Domain.Entities;

namespace Nuts.Application.Products;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(CancellationToken ct = default);
    Task<List<Product>> GetAvailableAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(Product product);
    Task<Product?> GetByNameAsync(string name, CancellationToken ct = default);
    void Remove(Product product);
    Task RemoveVariantsAsync(Guid productId, CancellationToken ct = default);
    Task AddVariantRawAsync(Guid productId, string weight, decimal price, int sortOrder, CancellationToken ct = default);
}
