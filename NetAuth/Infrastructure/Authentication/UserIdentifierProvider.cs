using Ardalis.GuardClauses;
using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth.Infrastructure.Authentication;

internal sealed class UserIdentifierProvider(
    IHttpContextAccessor httpContextAccessor
) : IUserIdentifierProvider
{
    public Guid UserId => GetUserId(httpContextAccessor);

    private static Guid GetUserId(IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User.GetUserIdOrNull();
        return Guard.Against.Null(userId);
    }
}