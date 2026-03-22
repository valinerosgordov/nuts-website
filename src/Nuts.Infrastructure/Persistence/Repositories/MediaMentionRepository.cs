using Microsoft.EntityFrameworkCore;
using Nuts.Application.Media;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Persistence.Repositories;

internal sealed class MediaMentionRepository(AppDbContext db) : IMediaMentionRepository
{
    public Task<List<MediaMention>> GetAllAsync(CancellationToken ct = default) =>
        db.MediaMentions.OrderBy(m => m.SortOrder).ToListAsync(ct);

    public Task<List<MediaMention>> GetVisibleAsync(CancellationToken ct = default) =>
        db.MediaMentions.Where(m => m.IsVisible).OrderBy(m => m.SortOrder).ToListAsync(ct);

    public Task<MediaMention?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.MediaMentions.FirstOrDefaultAsync(m => m.Id == id, ct);

    public void Add(MediaMention mention) => db.MediaMentions.Add(mention);

    public void Remove(MediaMention mention) => db.MediaMentions.Remove(mention);
}
