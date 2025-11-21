using System.Diagnostics.Contracts;
using LanguageExt;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public sealed class Password : ValueObject
{
    public const int MinLength = 6;

    private static readonly Func<char, bool> IsLower = c => c is >= 'a' and <= 'z';
    private static readonly Func<char, bool> IsUpper = c => c is >= 'A' and <= 'Z';
    private static readonly Func<char, bool> IsDigit = c => c is >= '0' and <= '9';
    private static readonly Func<char, bool> IsNonAlphaNumeric = c => !(IsLower(c) || IsUpper(c) || IsDigit(c));

    public required string Value { get; init; }

    private Password()
    {
    }

    protected override IEnumerable<object> GetAtomicValues() => [Value];

    public static implicit operator string(Password password) => password.Value;

    [Pure]
    public static Either<DomainError, Password> Create(string password) =>
        password switch
        {
            _ when string.IsNullOrWhiteSpace(password)
                => UsersDomainErrors.Password.NullOrEmpty,

            { Length: < MinLength }
                => UsersDomainErrors.Password.TooShort,

            _ when !password.Any(IsUpper)
                => UsersDomainErrors.Password.MissingUppercaseLetter,

            _ when !password.Any(IsLower)
                => UsersDomainErrors.Password.MissingLowercaseLetter,

            _ when !password.Any(IsDigit)
                => UsersDomainErrors.Password.MissingDigit,

            _ when !password.Any(IsNonAlphaNumeric)
                => UsersDomainErrors.Password.MissingNonAlphaNumeric,

            _ => new Password { Value = password }
        };
}