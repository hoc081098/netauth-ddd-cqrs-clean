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

    /// <summary>
    /// Updates the user's roles with the specified collection.
    /// </summary>
    /// <param name="roles">The new roles to assign to the user. Must not be null or empty.</param>
    /// <param name="isPerformedByAdmin">
    /// Indicates whether this operation is performed by an administrator.
    /// If false, additional validation prevents users from modifying their own admin roles or granting themselves admin access.
    /// </param>
    /// <returns>
    /// Returns <see cref="Unit.Default"/> if the roles were successfully updated,
    /// or a <see cref="DomainError"/> if validation fails.
    /// </returns>
    /// <remarks>
    /// This method ensures that:
    /// <list type="bullet">
    /// <item>The roles collection is not empty</item>
    /// <item>Duplicate roles are removed</item>
    /// <item>Non-admin users cannot modify their own admin roles</item>
    /// <item>Non-admin users cannot grant themselves admin privileges</item>
    /// <item>A <see cref="UserRolesChangedDomainEvent"/> is raised when roles actually change</item>
    /// </list>
    /// </remarks>
    public Either<DomainError, Unit> SetRoles(IReadOnlyList<Role> roles, bool isPerformedByAdmin)
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
                isPerformedByAdmin: isPerformedByAdmin,
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

    /// <summary>
    /// Validates that admin role changes are allowed based on who is performing the operation.
    /// </summary>
    /// <param name="isPerformedByAdmin">True if an administrator is performing this operation.</param>
    /// <param name="currentRoleIds">The user's current role IDs before the change.</param>
    /// <param name="newRoleIds">The proposed new role IDs after the change.</param>
    /// <returns>
    /// Returns <see cref="Unit.Default"/> if the admin role change is allowed,
    /// or a <see cref="DomainError"/> if the change violates security rules.
    /// </returns>
    /// <remarks>
    /// Security rules:
    /// <list type="bullet">
    /// <item>Administrators can perform any role changes</item>
    /// <item>Non-admin users cannot remove their own admin role (prevents privilege escalation attacks)</item>
    /// <item>Non-admin users cannot grant themselves admin role</item>
    /// </list>
    /// </remarks>
    private static Either<DomainError, Unit> EnsureAdminRoleChangeIsAllowed(
        bool isPerformedByAdmin,
        IReadOnlySet<RoleId> currentRoleIds,
        IReadOnlySet<RoleId> newRoleIds)
    {
        if (isPerformedByAdmin)
        {
            return Unit.Default;
        }

        if (currentRoleIds.Contains(RoleId.AdministratorId))
        {
            return UsersDomainErrors.User.CannotModifyOwnAdminRoles;
        }

        if (newRoleIds.Contains(RoleId.AdministratorId))
        {
            return UsersDomainErrors.User.CannotGrantAdminRole;
        }

        return Unit.Default;
    }
}