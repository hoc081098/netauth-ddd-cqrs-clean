using LanguageExt;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;
using static LanguageExt.Prelude;

namespace NetAuth.Application.Users.GetRoles;

internal sealed class GetRolesQueryHandler(
    IRoleRepository roleRepository
) : IQueryHandler<GetRolesQuery, GetRolesResult>
{
    public Task<Either<DomainError, GetRolesResult>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var toGetRolesResult = ToGetRolesResult;
        var right = Right<DomainError, GetRolesResult>;

        return roleRepository
            .GetAllRolesAsync(cancellationToken)
            .Map(toGetRolesResult.Compose(right));
    }

    private static GetRolesResult ToGetRolesResult(IReadOnlyList<Role> roles)
    {
        var rolesResponses = roles
            .Select(role => new RoleResponse(
                Id: role.Id,
                Name: role.Name))
            .ToArray();
        return new GetRolesResult(rolesResponses);
    }
}