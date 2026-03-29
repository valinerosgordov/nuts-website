using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Weight).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Price).HasPrecision(10, 2).IsRequired();
        builder.Property(v => v.SortOrder).IsRequired();
    }
}
