using Nuts.Domain.Common;

namespace Nuts.Domain.Entities;

public sealed class User : AggregateRoot<Guid>
{
    private User() { }

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? LastLoginAt { get; private set; }

    public static Result<User> Create(string email, string passwordHash, string fullName, string? phone)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<User>.Failure(new Error("User.EmailRequired", "Email is required."));

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result<User>.Failure(new Error("User.PasswordRequired", "Password is required."));

        if (string.IsNullOrWhiteSpace(fullName))
            return Result<User>.Failure(new Error("User.NameRequired", "Name is required."));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Phone = phone?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result UpdateProfile(string fullName, string? phone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure(new Error("User.NameRequired", "Name is required."));

        FullName = fullName.Trim();
        Phone = phone?.Trim();
        return Result.Success();
    }

    public void UpdateLastLogin() => LastLoginAt = DateTime.UtcNow;
}
