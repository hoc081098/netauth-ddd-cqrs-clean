using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public static class UsersDomainErrors
{
    public static class User
    {
        public static DomainError DuplicateEmail =>
            new(
                code: "User.DuplicateEmail",
                message: "The email is already in use.",
                type: DomainError.ErrorType.Conflict);
    }

    public static class Email
    {
        public static DomainError NullOrEmpty =>
            new(
                code: "User.Email.NullOrEmpty",
                message: "The email is required.",
                type: DomainError.ErrorType.Validation);

        public static DomainError TooLong =>
            new(
                code: "User.Email.TooLong",
                message: $"The email cannot exceed {Users.Email.MaxLength} characters.",
                type: DomainError.ErrorType.Validation);

        public static DomainError InvalidFormat =>
            new(
                code: "User.Email.InvalidFormat",
                message: "The email format is invalid.",
                type: DomainError.ErrorType.Validation);
    }

    public static class Username
    {
        public static DomainError NullOrEmpty =>
            new(
                code: "User.Username.NullOrEmpty",
                message: "The username is required.",
                type: DomainError.ErrorType.Validation);

        public static DomainError TooShort =>
            new(
                code: "User.Username.TooShort",
                message: $"The username must be at least {Users.Username.MinLength} characters.",
                type: DomainError.ErrorType.Validation);

        public static DomainError TooLong =>
            new(
                code: "User.Username.TooLong",
                message: $"The username cannot exceed {Users.Username.MaxLength} characters.",
                type: DomainError.ErrorType.Validation);

        public static DomainError InvalidFormat =>
            new(
                code: "User.Username.InvalidFormat",
                message: "The username can only contain letters, numbers, underscores, and hyphens.",
                type: DomainError.ErrorType.Validation);
    }

    public static class Password
    {
        public static DomainError NullOrEmpty =>
            new(
                code: "User.Password.NullOrEmpty",
                message: "The password is required.",
                type: DomainError.ErrorType.Validation);

        public static DomainError TooShort =>
            new(
                code: "User.Password.TooShort",
                message: $"The password must be at least {Users.Password.MinLength} characters.",
                type: DomainError.ErrorType.Validation);

        public static DomainError MissingUppercaseLetter =>
            new(
                code: "User.Password.MissingUppercaseLetter",
                message: "The password requires at least one uppercase letter.",
                type: DomainError.ErrorType.Validation);

        public static DomainError MissingLowercaseLetter =>
            new(
                code: "User.Password.MissingLowercaseLetter",
                message: "The password requires at least one lowercase letter.",
                type: DomainError.ErrorType.Validation);

        public static DomainError MissingDigit =>
            new(
                code: "User.Password.MissingDigit",
                message: "The password requires at least one digit.",
                type: DomainError.ErrorType.Validation);

        public static DomainError MissingNonAlphaNumeric =>
            new(
                code: "User.Password.MissingNonAlphaNumeric",
                message: "The password requires at least one non-alphanumeric.",
                type: DomainError.ErrorType.Validation);
    }
}