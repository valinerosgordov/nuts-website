using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Configurations;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Title).HasMaxLength(300).IsRequired();
        builder.Property(p => p.MetaDescription).HasMaxLength(500);
        builder.Property(p => p.ContentJson).HasMaxLength(50000);
        builder.Ignore(p => p.DomainEvents);
    }
}
