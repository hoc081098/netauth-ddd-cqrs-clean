using LanguageExt;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;
using Unit = MediatR.Unit;

namespace NetAuth.Application.Users.SetUserRoles;

internal sealed class SetUserRolesCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<SetUserRolesCommand, Unit>
{
    public async Task<Either<DomainError, Unit>> Handle(SetUserRolesCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Retrieve user with roles
        var user = await userRepository.GetByIdAsyncWithRoles(command.UserId, cancellationToken);
        if (user is null)
        {
            return UsersDomainErrors.User.NotFound;
        }

        // 2. Retrieve roles from repository
        var roleIds = command.RoleIds
            .Select(raw => new RoleId(raw))
            .ToHashSet();
        var roles = await roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);

        // 3. Validate all requested roles exist
        if (!roleIds.SetEquals(roles.Select(r => r.Id)))
        {
            return UsersDomainErrors.User.OneOrMoreRolesNotFound;
        }

        // 4. Set roles and save changes
        return await user
            .SetRoles(roles, command.RoleChangeActor)
            .MapAsync(async _ =>
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Unit.Value;
            });
    }
}