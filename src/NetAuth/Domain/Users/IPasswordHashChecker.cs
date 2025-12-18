namespace NetAuth.Domain.Users;

public interface IPasswordHashChecker
{
    /// <summary>
    /// Check if the provided password matches the hashed password.
    /// </summary>
    /// <param name="passwordHash">The password hash</param>
    /// <param name="providedPassword">The provided password</param>
    /// <returns></returns>
    bool IsMatch(string passwordHash, string providedPassword);
}