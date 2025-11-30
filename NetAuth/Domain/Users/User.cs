using Ardalis.GuardClauses;
using JetBrains.Annotations;
using NetAuth.Domain.Core.Abstractions;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Domain.Users;

public sealed class User : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private string _passwordHash = string.Empty;
    private readonly List<Role> _roles = [];

    public Email Email { get; } = null!;
    public Username Username { get; } = null!;

    /// <remarks>Required by EF Core.</remarks>
    [UsedImplicitly]
    private User()
    {
    }

    private User(Guid id, Email email, Username username, string passwordHash)
        : base(id)
    {
        Guard.Against.NullOrWhiteSpace(email);
        Guard.Against.NullOrWhiteSpace(username);
        Guard.Against.NullOrWhiteSpace(passwordHash);

        Email = email;
        Username = username;
        _passwordHash = passwordHash;
    }

    // Navigation property
    public IReadOnlyList<Role> Roles => [.._roles];

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

    [System.Diagnostics.Contracts.Pure]
    public static User Create(Email email, Username username, string passwordHash)
    {
        var user = new User(id: Guid.CreateVersion7(),
            email: email,
            username: username,
            passwordHash: passwordHash);

        user._roles.Add(Role.Member);
        user.AddDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }
}