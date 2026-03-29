using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Configurations;

internal sealed class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.DiscountPercent).IsRequired();
        builder.Property(p => p.MaxUses).IsRequired();
        builder.Property(p => p.CurrentUses).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Ignore(p => p.DomainEvents);
    }
}
