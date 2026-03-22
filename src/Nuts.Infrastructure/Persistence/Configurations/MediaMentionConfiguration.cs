using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Configurations;

internal sealed class MediaMentionConfiguration : IEntityTypeConfiguration<MediaMention>
{
    public void Configure(EntityTypeBuilder<MediaMention> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Source).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Quote).HasMaxLength(1000).IsRequired();
        builder.Property(m => m.Url).HasMaxLength(500);
        builder.Property(m => m.LogoPath).HasMaxLength(500);
        builder.Ignore(m => m.DomainEvents);
    }
}
