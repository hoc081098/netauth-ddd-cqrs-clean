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
        GetRolesQuery query,
        CancellationToken cancellationToken) =>
        roleRepository
            .GetAllRolesAsync(cancellationToken)
            .Map(ToGetRolesResult)
            .Map(Right<DomainError, GetRolesResult>);

    private static GetRolesResult ToGetRolesResult(IReadOnlyList<Role> roles)
    {
        var rolesResponses = roles
            .Select(role => role.ToRoleDto())
            .ToArray();
        return new GetRolesResult(rolesResponses);
    }
}