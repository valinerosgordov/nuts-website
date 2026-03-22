using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class Page : AggregateRoot<Guid>
{
    private Page() { }

    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? MetaDescription { get; private set; }
    public string ContentJson { get; private set; } = "{}";
    public bool IsPublished { get; private set; } = true;
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    public static Result<Page> Create(string slug, string title, string? metaDescription = null)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Result<Page>.Failure(new Error("Page.SlugRequired", "Slug is required."));

        if (string.IsNullOrWhiteSpace(title))
            return Result<Page>.Failure(new Error("Page.TitleRequired", "Title is required."));

        return new Page
        {
            Id = Guid.NewGuid(),
            Slug = slug.Trim().ToLowerInvariant(),
            Title = title.Trim(),
            MetaDescription = metaDescription?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result Update(string title, string? metaDescription, string contentJson, bool isPublished)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure(new Error("Page.TitleRequired", "Title is required."));

        Title = title.Trim();
        MetaDescription = metaDescription?.Trim();
        ContentJson = contentJson;
        IsPublished = isPublished;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }
}
