using Microsoft.AspNetCore.Authorization;
using NetAuth.Infrastructure.Authentication;

namespace NetAuth.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasRequirementPermission = context.User.HasPermission(requirement.Permission);
        if (hasRequirementPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}