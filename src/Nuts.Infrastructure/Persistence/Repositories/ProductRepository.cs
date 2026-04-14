using Microsoft.EntityFrameworkCore;
using Nuts.Application.Products;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<List<Product>> GetAllAsync(CancellationToken ct = default) =>
        db.Products.Include(p => p.Variants).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync(ct);

    public Task<List<Product>> GetAvailableAsync(CancellationToken ct = default) =>
        db.Products.Include(p => p.Variants).Where(p => p.IsAvailable).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync(ct);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Name == name, ct);

    public void Add(Product product) => db.Products.Add(product);

    public void Remove(Product product) => db.Products.Remove(product);

    public async Task RemoveVariantsAsync(Guid productId, CancellationToken ct = default)
    {
        // Use raw SQL to avoid EF change tracker conflicts
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM ProductVariants WHERE ProductId = {0}", [productId], ct);

        // Detach any tracked variant entities to prevent stale state
        var trackedVariants = db.ChangeTracker.Entries<ProductVariant>()
            .Where(e => e.Entity.ProductId == productId)
            .ToList();
        foreach (var entry in trackedVariants)
            entry.State = EntityState.Detached;
    }
}
