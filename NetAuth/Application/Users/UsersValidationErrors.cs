using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Users;

public static class UsersValidationErrors
{
    public static class Register
    {
        public static DomainError UsernameIsRequired =>
            new(
                code: "Register.UsernameIsRequired",
                message: "Username is required.",
                type: DomainError.ErrorType.Validation);

        public static DomainError EmailIsRequired =>
            new(
                code: "Register.EmailIsRequired",
                message: "Email is required.",
                type: DomainError.ErrorType.Validation);

        public static DomainError PasswordIsRequired =>
            new(
                code: "Register.PasswordIsRequired",
                message: "Password is required.",
                type: DomainError.ErrorType.Validation);
    }
    
    public static class Login
    {
        public static DomainError EmailIsRequired =>
            new(
                code: "Login.EmailIsRequired",
                message: "Email is required.",
                type: DomainError.ErrorType.Validation);

        public static DomainError PasswordIsRequired =>
            new(
                code: "Login.PasswordIsRequired",
                message: "Password is required.",
                type: DomainError.ErrorType.Validation);
        
        public static DomainError DeviceIdIsRequired =>
            new(
                code: "Login.DeviceIdIsRequired",
                message: "Device id is required.",
                type: DomainError.ErrorType.Validation);
    }
    
    public static class LoginWithRefreshToken
    {
        public static DomainError RefreshTokenIsRequired =>
            new(
                code: "Login.RefreshTokenIsRequired",
                message: "Refresh token is required.",
                type: DomainError.ErrorType.Validation);

        public static DomainError DeviceIdIsRequired =>
            new(
                code: "Login.DeviceIdIsRequired",
                message: "Device id is required.",
                type: DomainError.ErrorType.Validation);
    }
}