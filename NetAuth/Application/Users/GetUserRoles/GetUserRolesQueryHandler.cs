using LanguageExt;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;
using static LanguageExt.Prelude;

namespace NetAuth.Application.Users.GetUserRoles;

internal sealed class GetUserRolesQueryHandler(
    IRoleRepository roleRepository) : IQueryHandler<GetUserRolesQuery, GetUserRolesResult>
{
    public Task<Either<DomainError, GetUserRolesResult>> Handle(GetUserRolesQuery query,
        CancellationToken cancellationToken) =>
        roleRepository
            .GetRolesByUserIdAsync(query.UserId, cancellationToken)
            .Map(ToGetUserRolesResult)
            .Map(Right<DomainError, GetUserRolesResult>);

    private static GetUserRolesResult ToGetUserRolesResult(IReadOnlyList<Role> roles) => new(roles);
}