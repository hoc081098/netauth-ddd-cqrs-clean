namespace NetAuth;

[Obsolete("LegacyUser is deprecated and will be removed in future versions. Use the new User entity instead.")]
public record LegacyUser(Guid Id, string Username, string Email, string PasswordHash);