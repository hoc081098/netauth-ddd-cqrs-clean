using NetAuth.Domain.Core.Events;

namespace NetAuth.Domain.Users.DomainEvents;

public sealed record UserRolesChangedDomainEvent(
    Guid UserId,
    // Must use HashSet here to avoid the exception when deserializing:
    //     System.NotSupportedException: The collection type 'System.Collections.Generic.IReadOnlySet`1[NetAuth.Domain.Users.RoleId]'
    //     is abstract, an interface, or is read only, and could not be instantiated and populated.
    HashSet<RoleId> OldRoleIds,
    HashSet<RoleId> NewRoleIds) : IDomainEvent;