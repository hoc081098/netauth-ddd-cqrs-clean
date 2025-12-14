using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public static class UsersDomainErrors
{
    public static class User
    {
        public static readonly DomainError DuplicateEmail = new(
            code: "User.DuplicateEmail",
            message: "The email is already in use.",
            type: DomainError.ErrorType.Conflict);

        public static readonly DomainError InvalidCredentials = new(
            code: "User.InvalidCredentials",
            message: "The specified email or password is incorrect.",
            type: DomainError.ErrorType.Unauthorized);

        public static readonly DomainError NotFound = new(
            code: "User.NotFound",
            message: "The user was not found.",
            type: DomainError.ErrorType.NotFound);

        public static readonly DomainError OneOrMoreRolesNotFound = new(
            code: "User.OneOrMoreRolesNotFound",
            message: " One or more specified roles were not found.",
            type: DomainError.ErrorType.NotFound);

        public static readonly DomainError EmptyRolesNotAllowed = new(
            code: "User.EmptyRolesNotAllowed",
            message: "A user must have at least one role assigned.",
            type: DomainError.ErrorType.Validation);
        
        public static readonly DomainError CannotModifyOwnAdminRoles = new(
            code: "User.CannotModifyOwnAdminRoles",
            message: "An administrator cannot modify their own admin roles.",
            type: DomainError.ErrorType.Validation);
        
        public static readonly DomainError CannotGrantAdminRole = new(
            code: "User.CannotGrantAdminRole",
            message: "Only an administrator can grant the admin role to a user.",
            type: DomainError.ErrorType.Unauthorized);
    }

    public static class RefreshToken
    {
        public static readonly DomainError Invalid = new(
            code: "RefreshToken.Invalid",
            message: "The refresh token is invalid.",
            type: DomainError.ErrorType.Unauthorized);

        public static readonly DomainError Expired = new(
            code: "RefreshToken.Expired",
            message: "The refresh token has expired.",
            type: DomainError.ErrorType.Unauthorized);

        public static readonly DomainError Revoked = new(
            code: "RefreshToken.Revoked",
            message: "The refresh token has been revoked.",
            type: DomainError.ErrorType.Unauthorized);

        public static readonly DomainError InvalidDevice = new(
            code: "RefreshToken.InvalidDevice",
            message: "The refresh token was used from an invalid device.",
            type: DomainError.ErrorType.Unauthorized);
    }

    public static class Email
    {
        public static readonly DomainError NullOrEmpty = new(
            code: "User.Email.NullOrEmpty",
            message: "The email is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError TooLong = new(
            code: "User.Email.TooLong",
            message: $"The email cannot exceed {Users.Email.MaxLength} characters.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError InvalidFormat = new(
            code: "User.Email.InvalidFormat",
            message: "The email format is invalid.",
            type: DomainError.ErrorType.Validation);
    }

    public static class Username
    {
        public static readonly DomainError NullOrEmpty = new(
            code: "User.Username.NullOrEmpty",
            message: "The username is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError TooShort = new(
            code: "User.Username.TooShort",
            message: $"The username must be at least {Users.Username.MinLength} characters.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError TooLong = new(
            code: "User.Username.TooLong",
            message: $"The username cannot exceed {Users.Username.MaxLength} characters.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError InvalidFormat = new(
            code: "User.Username.InvalidFormat",
            message: "The username can only contain letters, numbers, underscores, and hyphens.",
            type: DomainError.ErrorType.Validation);
    }

    public static class Password
    {
        public static readonly DomainError NullOrEmpty = new(
            code: "User.Password.NullOrEmpty",
            message: "The password is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError TooShort = new(
            code: "User.Password.TooShort",
            message: $"The password must be at least {Users.Password.MinLength} characters.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError MissingUppercaseLetter = new(
            code: "User.Password.MissingUppercaseLetter",
            message: "The password requires at least one uppercase letter.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError MissingLowercaseLetter = new(
            code: "User.Password.MissingLowercaseLetter",
            message: "The password requires at least one lowercase letter.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError MissingDigit = new(
            code: "User.Password.MissingDigit",
            message: "The password requires at least one digit.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError MissingNonAlphaNumeric = new(
            code: "User.Password.MissingNonAlphaNumeric",
            message: "The password requires at least one non-alphanumeric.",
            type: DomainError.ErrorType.Validation);
    }
}