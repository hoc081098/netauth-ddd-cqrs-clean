using MediatR;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Users;

namespace NetAuth.Application.Users.SetUserRoles;

public sealed record SetUserRolesCommand(
    Guid UserId,
    IReadOnlyList<int> RoleIds,
    RoleChangeActor RoleChangeActor
) : ICommand<Unit>;