using Microsoft.AspNetCore.Authorization;
using NetAuth.Infrastructure.Authentication;

namespace NetAuth.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasRequirementPermission = context.User
            .HasClaim(claim =>
                string.Equals(claim.Type, CustomClaimTypes.Permission, StringComparison.Ordinal) &&
                string.Equals(claim.Value, requirement.Permission, StringComparison.Ordinal));
        if (hasRequirementPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}