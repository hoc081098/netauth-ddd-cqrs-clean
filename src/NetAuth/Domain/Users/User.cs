using Ardalis.GuardClauses;
using JetBrains.Annotations;
using LanguageExt;
using NetAuth.Domain.Core.Abstractions;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.Domain.Users;

/// <summary>
/// Represents a user in the system. This is the aggregate root for user-related operations.
/// </summary>
/// <remarks>
/// <para>
/// The User aggregate manages:
/// <list type="bullet">
/// <item><description>User identity (email, username)</description></item>
/// <item><description>Authentication credentials (password hash)</description></item>
/// <item><description>Role-based access control with security checks</description></item>
/// <item><description>Audit trail (created, modified, deleted timestamps)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Invariants:</b>
/// <list type="bullet">
/// <item><description>User must always have at least one role</description></item>
/// <item><description>Email and username must be valid and non-empty</description></item>
/// <item><description>Password hash must be non-empty</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class User : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private readonly string _passwordHash = string.Empty;
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

    /// <summary>
    /// Verifies if the provided password matches the stored password hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="passwordHashChecker">The service to check password hashes.</param>
    /// <returns><c>true</c> if the password matches; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Returns <c>false</c> if the password is null, empty, or whitespace,
    /// or if the hash verification fails.
    /// </remarks>
    public bool VerifyPasswordHash(string password, IPasswordHashChecker passwordHashChecker)
        => !string.IsNullOrWhiteSpace(password) &&
           passwordHashChecker.IsMatch(passwordHash: _passwordHash, providedPassword: password);

    /// <summary>
    /// Creates a new user with the specified credentials.
    /// </summary>
    /// <param name="email">The validated email value object for the user.</param>
    /// <param name="username">The validated username value object for the user.</param>
    /// <param name="passwordHash">The hashed password (must be non-empty; use <see cref="Application.Abstractions.Cryptography.IPasswordHasher"/> to generate).</param>
    /// <returns>A new <see cref="User"/> instance with the Member role assigned.</returns>
    /// <remarks>
    /// <para>
    /// This method:
    /// <list type="bullet">
    /// <item><description>Generates a new GUIDv7 for the user ID</description></item>
    /// <item><description>Assigns the default Member role</description></item>
    /// <item><description>Raises a <see cref="UserCreatedDomainEvent"/></description></item>
    /// </list>
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Updates the user's roles with security checks to prevent privilege escalation.
    /// </summary>
    /// <param name="roles">The new set of roles to assign to the user.</param>
    /// <param name="actor">The actor performing the role change (User, Privileged, or System).</param>
    /// <returns>
    /// An Either containing Unit on success, or a DomainError if:
    /// <list type="bullet">
    /// <item><description>Roles list is empty (<see cref="UsersDomainErrors.User.EmptyRolesNotAllowed"/>)</description></item>
    /// <item><description>Non-privileged user tries to modify their own admin role (<see cref="UsersDomainErrors.User.CannotModifyOwnAdminRoles"/>)</description></item>
    /// <item><description>Non-privileged user tries to grant admin role (<see cref="UsersDomainErrors.User.CannotGrantAdminRole"/>)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Security Rules:</b>
    /// <list type="bullet">
    /// <item><description>System and Privileged actors can make any role changes</description></item>
    /// <item><description>Regular users cannot modify their own admin roles</description></item>
    /// <item><description>Regular users cannot grant admin roles to others</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Behavior:</b>
    /// <list type="bullet">
    /// <item><description>Duplicate roles are automatically removed</description></item>
    /// <item><description>If roles don't actually change, operation succeeds without event emission</description></item>
    /// <item><description>On success, raises <see cref="UserRolesChangedDomainEvent"/></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public Either<DomainError, Unit> SetRoles(IReadOnlyList<Role> roles,
        RoleChangeActor actor)
    {
        // 1. Validate input
        if (roles is null or { Count: 0 })
        {
            return UsersDomainErrors.User.EmptyRolesNotAllowed;
        }

        // 2. Remove duplicates if any
        var newRoles = roles.DistinctBy(r => r.Id).ToArray();

        // 3. Check if roles are actually changing
        var newRoleIds = newRoles.Select(r => r.Id).ToHashSet();
        var currentRoleIds = _roles.Select(r => r.Id).ToHashSet();
        if (currentRoleIds.SetEquals(newRoleIds))
        {
            return Unit.Default;
        }

        // 4. Ensure admin role changes are allowed
        return EnsureAdminRoleChangeIsAllowed(
                actor: actor,
                currentRoleIds: currentRoleIds,
                newRoleIds: newRoleIds)
            .Map(_ =>
            {
                // 5. Apply changes
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

    /// <summary>
    /// Validates that the actor is allowed to make admin-related role changes.
    /// </summary>
    /// <param name="actor">The actor performing the role change.</param>
    /// <param name="currentRoleIds">The user's current role IDs.</param>
    /// <param name="newRoleIds">The proposed new role IDs.</param>
    /// <returns>
    /// <see cref="Unit.Default"/> if allowed; otherwise, a <see cref="DomainError"/> describing the violation.
    /// </returns>
    /// <remarks>
    /// Uses pattern matching to enforce security rules:
    /// <list type="bullet">
    /// <item><description>Privileged/System actors: allowed to make any changes</description></item>
    /// <item><description>User actor with admin role: cannot modify own admin roles</description></item>
    /// <item><description>User actor without admin: cannot grant admin role</description></item>
    /// </list>
    /// </remarks>
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
            (RoleChangeActor.Privileged or RoleChangeActor.System, _, _) => Unit.Default,

            // Non-privileged actor currently has admin => cannot modify own admin roles
            (RoleChangeActor.User, true, _) => UsersDomainErrors.User.CannotModifyOwnAdminRoles,

            // Non-privileged actor tries to grant admin => forbidden
            (RoleChangeActor.User, false, true) => UsersDomainErrors.User.CannotGrantAdminRole,

            // Otherwise ok
            _ => Unit.Default
        };
    }
}
