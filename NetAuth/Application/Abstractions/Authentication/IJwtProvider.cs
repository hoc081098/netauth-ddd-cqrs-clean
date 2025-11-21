using NetAuth.Domain.Users;

namespace NetAuth.Application.Abstractions.Authentication;

public interface IJwtProvider
{
    [Obsolete("Use CreateJwtToken(User user) instead.")]
    string CreateJwtToken(LegacyUser user);

    string CreateJwtToken(User user);
}