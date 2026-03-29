using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Configurations;

internal sealed class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(1000).IsRequired();
        builder.Property(b => b.ButtonText).HasMaxLength(100);
        builder.Property(b => b.ButtonUrl).HasMaxLength(500);
        builder.Property(b => b.ImageUrl).HasMaxLength(500);
        builder.Property(b => b.DelaySeconds).IsRequired();
        builder.Property(b => b.IsActive).IsRequired();
        builder.Ignore(b => b.DomainEvents);
    }
}
