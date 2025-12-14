using MediatR;
using NetAuth.Application.Abstractions.Messaging;

namespace NetAuth.Application.Users.SetUserRoles;

public sealed record SetUserRolesCommand(
    Guid UserId,
    IReadOnlyList<int> RoleIds
) : ICommand<Unit>;