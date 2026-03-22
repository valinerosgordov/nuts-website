using Nuts.Domain.Entities;

namespace Nuts.Application.Media;

public interface IMediaMentionRepository
{
    Task<List<MediaMention>> GetAllAsync(CancellationToken ct = default);
    Task<List<MediaMention>> GetVisibleAsync(CancellationToken ct = default);
    Task<MediaMention?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(MediaMention mention);
    void Remove(MediaMention mention);
}
