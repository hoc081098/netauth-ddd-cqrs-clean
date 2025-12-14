using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.Users.GetUserRoles;

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<GetUserRolesResult>;

public sealed record GetUserRolesResult(IReadOnlyList<RoleResponse> Roles);