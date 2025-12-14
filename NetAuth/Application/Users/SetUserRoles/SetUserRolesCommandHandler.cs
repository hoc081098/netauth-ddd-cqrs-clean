using LanguageExt;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;
using Unit = MediatR.Unit;

namespace NetAuth.Application.Users.SetUserRoles;

internal sealed class SetUserRolesCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext
) : ICommandHandler<SetUserRolesCommand, Unit>
{
    public async Task<Either<DomainError, Unit>> Handle(SetUserRolesCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsyncWithRoles(command.UserId, cancellationToken);
        if (user is null)
        {
            return UsersDomainErrors.User.NotFound;
        }

        var roleIds = command.RoleIds
            .Select(raw => new RoleId(raw))
            .ToHashSet();
        var roles = await roleRepository.GetRolesByIdsAsync(roleIds, cancellationToken);

        if (!roleIds.SetEquals(roles.Select(r => r.Id)))
        {
            return UsersDomainErrors.User.OneOrMoreRolesNotFound;
        }

        return await user
            .SetRoles(roles: roles, actor: RoleChangeActor.Administrator)
            .MapAsync(async _ =>
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Unit.Value;
            });
    }
}