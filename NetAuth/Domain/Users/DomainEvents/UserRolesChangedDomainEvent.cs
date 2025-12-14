using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

public sealed record UserRolesChangedDomainEvent(
    Guid UserId,
    IReadOnlySet<RoleId> OldRoleIds,
    IReadOnlySet<RoleId> NewRoleIds) : IDomainEvent;