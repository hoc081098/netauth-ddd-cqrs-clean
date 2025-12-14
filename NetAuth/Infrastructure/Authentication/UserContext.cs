using System.Security.Claims;
using Ardalis.GuardClauses;
using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth.Infrastructure.Authentication;

internal sealed class UserContext(
    IHttpContextAccessor httpContextAccessor
) : IUserContext
{
    private ClaimsPrincipal? ClaimsPrincipal => httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            if (!IsAuthenticated)
            {
                throw new InvalidOperationException("The user is not authenticated.");
            }

            var userId = ClaimsPrincipal?.GetUserIdOrNull();

            return Guard.Against.Null(userId, exceptionCreator: () =>
                new InvalidOperationException("The user identifier claim is missing or invalid."));
        }
    }

    public bool IsAuthenticated => ClaimsPrincipal?.Identity?.IsAuthenticated == true;
}