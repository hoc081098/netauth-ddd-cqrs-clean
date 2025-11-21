using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Users;

public static class UsersValidationErrors
{
    public static class Register
    {
        public static DomainError FirstNameIsRequired =>
            new(code: "Register.FirstNameIsRequired", message: "First name is required.");

        public static DomainError EmailIsRequired =>
            new(code: "Register.EmailIsRequired", message: "Email is required.");

        public static DomainError PasswordIsRequired =>
            new(code: "Register.PasswordIsRequired", message: "Password is required.");
    }
}