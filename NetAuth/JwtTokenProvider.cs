using Microsoft.Extensions.Options;

namespace NetAuth;

public interface IJwtTokenProvider
{
    string CreateJwtToken(User user);
}

internal sealed class JwtTokenProvider(
    IOptions<JwtConfig> jwtConfig
) : IJwtTokenProvider
{
    public string CreateJwtToken(User user)
    {
        throw new NotImplementedException();
    }
}