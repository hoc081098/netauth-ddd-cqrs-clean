using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Users;

public static class UsersValidationErrors
{
    public static class Register
    {
        public static readonly DomainError UsernameIsRequired = new(
            code: "Register.UsernameIsRequired",
            message: "Username is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError EmailIsRequired = new(
            code: "Register.EmailIsRequired",
            message: "Email is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError PasswordIsRequired = new(
            code: "Register.PasswordIsRequired",
            message: "Password is required.",
            type: DomainError.ErrorType.Validation);
    }

    public static class Login
    {
        public static readonly DomainError EmailIsRequired = new(
            code: "Login.EmailIsRequired",
            message: "Email is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError PasswordIsRequired = new(
            code: "Login.PasswordIsRequired",
            message: "Password is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError DeviceIdIsRequired = new(
            code: "Login.DeviceIdIsRequired",
            message: "Device id is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError DeviceIdMustBeValidNonEmptyGuid = new(
            code: "Login.DeviceIdMustBeValidNonEmptyGuid",
            message: "Device id must be a valid non-empty UUID.",
            type: DomainError.ErrorType.Validation);
    }

    public static class LoginWithRefreshToken
    {
        public static readonly DomainError RefreshTokenIsRequired = new(
            code: "Login.RefreshTokenIsRequired",
            message: "Refresh token is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError DeviceIdIsRequired = new(
            code: "Login.DeviceIdIsRequired",
            message: "Device id is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError DeviceIdMustBeValidNonEmptyGuid = new(
            code: "Login.DeviceIdMustBeValidNonEmptyGuid",
            message: "Device id must be a valid non-empty UUID.",
            type: DomainError.ErrorType.Validation);
    }

    public static class SetUserRoles
    {
        public static readonly DomainError UserIdIsRequired = new(
            code: "SetUserRoles.UserIdIsRequired",
            message: "User id is required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError RoleIdsAreRequired = new(
            code: "SetUserRoles.RoleIdsAreRequired",
            message: "Role ids are required.",
            type: DomainError.ErrorType.Validation);

        public static readonly DomainError RoleChangeActorIsInvalid = new(
            code: "SetUserRoles.RoleChangeActorIsInvalid",
            message: "Role change actor is invalid.",
            type: DomainError.ErrorType.Validation);
    }
}