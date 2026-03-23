using System.Net;
using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class ContactRequest : AggregateRoot<Guid>
{
    private ContactRequest() { }

    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Message { get; private set; }
    public bool IsProcessed { get; private set; }
    public DateTime CreatedAt { get; private init; }

    public static Result<ContactRequest> Create(string name, string phone, string? email, string? message)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<ContactRequest>.Failure(new Error("Contact.NameRequired", "Name is required."));

        if (string.IsNullOrWhiteSpace(phone))
            return Result<ContactRequest>.Failure(new Error("Contact.PhoneRequired", "Phone is required."));

        return new ContactRequest
        {
            Id = Guid.NewGuid(),
            Name = WebUtility.HtmlEncode(name.Trim()),
            Phone = WebUtility.HtmlEncode(phone.Trim()),
            Email = email is not null ? WebUtility.HtmlEncode(email.Trim()) : null,
            Message = message is not null ? WebUtility.HtmlEncode(message.Trim()) : null,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed() => IsProcessed = true;
}
