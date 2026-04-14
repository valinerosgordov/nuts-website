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

    public void RemoveVariants(Product product)
    {
        // Use already-tracked variants from the loaded product to avoid concurrency issues
        var tracked = db.ChangeTracker.Entries<ProductVariant>()
            .Where(e => e.Entity.ProductId == product.Id)
            .Select(e => e.Entity)
            .ToList();

        if (tracked.Count > 0)
        {
            db.ProductVariants.RemoveRange(tracked);
        }
        else
        {
            // Fallback: query from DB
            var fromDb = db.ProductVariants.Where(v => v.ProductId == product.Id).ToList();
            db.ProductVariants.RemoveRange(fromDb);
        }
    }
}
