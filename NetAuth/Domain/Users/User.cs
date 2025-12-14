using Ardalis.GuardClauses;
using JetBrains.Annotations;
using LanguageExt;
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

    public Either<DomainError, Unit> SetRoles(IReadOnlyList<Role> roles,
        RoleChangeActor actor)
    {
        if (roles is null or { Count: 0 })
        {
            return UsersDomainErrors.User.EmptyRolesNotAllowed;
        }

        var newRoles = roles.DistinctBy(r => r.Id).ToArray();

        var newRoleIds = newRoles.Select(r => r.Id).ToHashSet();
        var currentRoleIds = _roles.Select(r => r.Id).ToHashSet();
        if (currentRoleIds.SetEquals(newRoleIds))
        {
            return Unit.Default;
        }

        return EnsureAdminRoleChangeIsAllowed(
                actor: actor,
                currentRoleIds: currentRoleIds,
                newRoleIds: newRoleIds)
            .Map(_ =>
            {
                _roles.Clear();
                _roles.AddRange(newRoles);

                AddDomainEvent(
                    new UserRolesChangedDomainEvent(
                        UserId: Id,
                        OldRoleIds: currentRoleIds,
                        NewRoleIds: newRoleIds
                    )
                );

                return Unit.Default;
            });
    }

    [Pure]
    private static Either<DomainError, Unit> EnsureAdminRoleChangeIsAllowed(
        RoleChangeActor actor,
        IReadOnlyCollection<RoleId> currentRoleIds,
        IReadOnlyCollection<RoleId> newRoleIds)
    {
        var hasAdminNow = currentRoleIds.Any(id => id.IsAdministrator);
        var hasAdminNext = newRoleIds.Any(id => id.IsAdministrator);

        return (actor, hasAdminNow, hasAdminNext) switch
        {
            // Privileged actors: allow anything
            (RoleChangeActor.Administrator or RoleChangeActor.System, _, _) => Unit.Default,

            // Non-privileged actor currently has admin => cannot modify own admin roles
            (RoleChangeActor.User, true, _) => UsersDomainErrors.User.CannotModifyOwnAdminRoles,

            // Non-privileged actor tries to grant admin => forbidden
            (RoleChangeActor.User, false, true) => UsersDomainErrors.User.CannotGrantAdminRole,

            // Otherwise ok
            _ => Unit.Default
        };
    }
}