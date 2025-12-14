using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users;

namespace NetAuth.Application.Users.GetUserRoles;

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<GetUserRolesResult>;

public sealed record GetUserRolesResult(IReadOnlyList<Role> Roles);