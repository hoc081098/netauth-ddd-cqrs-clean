namespace NetAuth;

public record JwtConfig(
    string SecretKey,
    string Issuer,
    string Audience,
    TimeSpan Expiration
);