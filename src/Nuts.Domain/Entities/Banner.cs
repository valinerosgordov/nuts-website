using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class Banner : AggregateRoot<Guid>
{
    private Banner() { }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ButtonText { get; private set; }
    public string? ButtonUrl { get; private set; }
    public string? ImageUrl { get; private set; }
    public int DelaySeconds { get; private set; } = 3;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private init; }

    public static Result<Banner> Create(
        string title,
        string description,
        string? buttonText = null,
        string? buttonUrl = null,
        int delaySeconds = 3)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Banner>.Failure(new Error("Banner.TitleRequired", "Title is required."));

        return new Banner
        {
            Id = Guid.NewGuid(),
            Title = System.Net.WebUtility.HtmlEncode(title.Trim()),
            Description = System.Net.WebUtility.HtmlEncode(description.Trim()),
            ButtonText = buttonText?.Trim(),
            ButtonUrl = buttonUrl?.Trim(),
            DelaySeconds = Math.Clamp(delaySeconds, 1, 30),
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result Update(
        string title,
        string description,
        string? buttonText,
        string? buttonUrl,
        int delaySeconds,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure(new Error("Banner.TitleRequired", "Title is required."));

        Title = System.Net.WebUtility.HtmlEncode(title.Trim());
        Description = System.Net.WebUtility.HtmlEncode(description.Trim());
        ButtonText = buttonText?.Trim();
        ButtonUrl = buttonUrl?.Trim();
        DelaySeconds = Math.Clamp(delaySeconds, 1, 30);
        IsActive = isActive;
        return Result.Success();
    }
}
