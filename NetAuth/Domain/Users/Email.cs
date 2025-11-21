using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using LanguageExt;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public sealed class Email : ValueObject
{
    public const int MaxLength = 256;

    private const string EmailRegexPattern = @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$";

    private static readonly Lazy<Regex> EmailRegex = new(() =>
        new(EmailRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));

    public required string Value { get; init; }

    private Email()
    {
    }

    protected override IEnumerable<object> GetAtomicValues() => [Value];

    public static implicit operator string(Email email) => email.Value;

    [Pure]
    public static Either<DomainError, Email> Create(string email) =>
        email switch
        {
            _ when string.IsNullOrWhiteSpace(email)
                => DomainErrors.Email.NullOrEmpty,

            { Length: > MaxLength }
                => DomainErrors.Email.TooLong,

            _ when !EmailRegex.Value.IsMatch(email)
                => DomainErrors.Email.InvalidFormat,

            _ => new Email { Value = email }
        };
}