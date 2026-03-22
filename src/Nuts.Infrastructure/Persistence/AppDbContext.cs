using Microsoft.EntityFrameworkCore;
using Nuts.Application.Common;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<MediaMention> MediaMentions => Set<MediaMention>();
    public DbSet<ContactRequest> ContactRequests => Set<ContactRequest>();
    public DbSet<Page> Pages => Set<Page>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
