namespace NetAuth;

public record User(Guid Id, string Username, string Email, string PasswordHash);
