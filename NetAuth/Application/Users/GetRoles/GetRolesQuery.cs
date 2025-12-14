using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users;

namespace NetAuth.Application.Users.GetRoles;

public sealed record GetRolesQuery : IQuery<GetRolesResult>;

public sealed record GetRolesResult(IReadOnlyList<RoleDto> Roles);
