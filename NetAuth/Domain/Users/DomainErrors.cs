using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public static class DomainErrors
{
    public static class Email
    {
        public static DomainError NullOrEmpty =>
            new(code: "Email.NullOrEmpty", message: "The email is required.");

        public static DomainError TooLong =>
            new(code: "Email.TooLong", message: $"The email cannot exceed {Users.Email.MaxLength} characters.");

        public static DomainError InvalidFormat =>
            new(code: "Email.InvalidFormat", message: "The email format is invalid.");
    }

    public static class Username
    {
        public static DomainError NullOrEmpty =>
            new(code: "Username.NullOrEmpty", message: "The username is required.");

        public static DomainError TooShort =>
            new(code: "Username.TooShort", message: $"The username must be at least {Users.Username.MinLength} characters.");

        public static DomainError TooLong =>
            new(code: "Username.TooLong", message: $"The username cannot exceed {Users.Username.MaxLength} characters.");

        public static DomainError InvalidFormat =>
            new(code: "Username.InvalidFormat", message: "The username can only contain letters, numbers, underscores, and hyphens.");
    }
}