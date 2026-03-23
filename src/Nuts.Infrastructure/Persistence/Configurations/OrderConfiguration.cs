using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasMaxLength(50).IsRequired();
        builder.Property(o => o.TotalAmount).HasPrecision(12, 2);
        builder.Property(o => o.ShippingAddress).HasMaxLength(500);
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(o => o.DomainEvents);
    }
}
