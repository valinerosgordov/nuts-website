using System.Net;
using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class MediaMention : AggregateRoot<Guid>
{
    private MediaMention() { }

    public string Source { get; private set; } = string.Empty;
    public string Quote { get; private set; } = string.Empty;
    public string? Url { get; private set; }
    public string? LogoPath { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsVisible { get; private set; } = true;
    public DateTime CreatedAt { get; private init; }

    public static Result<MediaMention> Create(string source, string quote, string? url = null)
    {
        if (string.IsNullOrWhiteSpace(source))
            return Result<MediaMention>.Failure(new Error("Media.SourceRequired", "Source is required."));

        if (string.IsNullOrWhiteSpace(quote))
            return Result<MediaMention>.Failure(new Error("Media.QuoteRequired", "Quote is required."));

        return new MediaMention
        {
            Id = Guid.NewGuid(),
            Source = WebUtility.HtmlEncode(source.Trim()),
            Quote = WebUtility.HtmlEncode(quote.Trim()),
            Url = url?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result Update(string source, string quote, string? url, bool isVisible, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(source))
            return Result.Failure(new Error("Media.SourceRequired", "Source is required."));

        Source = WebUtility.HtmlEncode(source.Trim());
        Quote = WebUtility.HtmlEncode(quote.Trim());
        Url = url?.Trim();
        IsVisible = isVisible;
        SortOrder = sortOrder;

        return Result.Success();
    }

    public void SetLogo(string? logoPath) => LogoPath = logoPath;
}
