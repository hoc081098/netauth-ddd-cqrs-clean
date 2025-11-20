namespace NetAuth;

public record LegacyUser(Guid Id, string Username, string Email, string PasswordHash);
