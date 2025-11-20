using NetAuth.Domain.Core.Abstractions;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Domain.Users;

public sealed class User : AggregateRoot, IAuditableEntity, ISoftDeletableEntity
{
    private string _passwordHash = string.Empty;

    public Email Email { get; } = null!;
    public Username Username { get; } = null!;

    /// <remarks>Required by EF Core.</remarks>
    private User()
    {
    }

    private User(Guid id, Email email, Username username, string passwordHash)
        : base(id)
    {
        Email = email;
        Username = username;
        _passwordHash = passwordHash;
    }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedOnUtc { get; }

    /// <inheritdoc />
    public bool IsDeleted { get; }

    public bool VerifyPasswordHash(string password, IPasswordHashChecker passwordHashChecker)
        => !string.IsNullOrWhiteSpace(password) &&
           passwordHashChecker.IsMatch(passwordHash: _passwordHash, providedPassword: password);

    public static User Create(Email email, Username username, string passwordHash)
    {
        var user = new User(id: Guid.CreateVersion7(),
            email: email,
            username: username,
            passwordHash: passwordHash);

        user.AddDomainEvent(new UserCreatedDomainEvent(user));

        return user;
    }
}