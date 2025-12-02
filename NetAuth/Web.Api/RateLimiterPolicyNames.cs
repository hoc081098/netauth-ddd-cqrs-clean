namespace NetAuth.Web.Api;

internal static class RateLimiterPolicyNames
{
    internal const string LoginLimiter = "login-limiter";
    internal const string RegisterLimiter = "register-limiter";
    internal const string RefreshTokenLimiter = "refresh-token-limiter";
}