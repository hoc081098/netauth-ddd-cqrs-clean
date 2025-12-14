using Ardalis.GuardClauses;
using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth.Infrastructure.Authentication;

internal sealed class UserContext(
    IHttpContextAccessor httpContextAccessor
) : IUserContext
{
    public Guid UserId => GetUserId(httpContextAccessor);

    public bool IsAuthenticated => httpContextAccessor
        .HttpContext
        ?.User
        .Identity
        ?.IsAuthenticated ?? false;

    private static Guid GetUserId(IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User.GetUserIdOrNull();
        return Guard.Against.Null(userId);
    }
}