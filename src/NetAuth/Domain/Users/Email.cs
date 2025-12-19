using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using LanguageExt;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public sealed class Email : ValueObject
{
    public const int MaxLength = 256;

    /// <summary>
    /// Regular expression used for pragmatic email syntax validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This pattern is intended to reject <b>obviously invalid</b> email addresses
    /// at the syntax level. It is <b>not</b> a full RFC 5322–compliant validator.
    /// </para>
    ///
    /// <para>
    /// The regex enforces:
    /// <list type="bullet">
    /// <item>Alphanumeric local-part with common safe symbols (<c>._%+-</c>)</item>
    /// <item>Exactly one <c>@</c> character</item>
    /// <item>Valid domain labels separated by dots</item>
    /// <item>Each domain label starts and ends with an alphanumeric character</item>
    /// <item>Top-level domain consists of letters only and is at least 2 characters long</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// The regex intentionally does <b>not</b> support:
    /// <list type="bullet">
    /// <item>Quoted local parts (e.g. <c>"user name"@example.com</c>)</item>
    /// <item>Unicode or internationalized domain names (IDN)</item>
    /// <item>Rare RFC edge cases</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <b>Note:</b> Passing this validation does not guarantee the email exists
    /// or can receive mail. Final verification must be done via email confirmation.
    /// </para>
    /// </remarks>
    private const string EmailRegexPattern =
        @"^[A-Za-z0-9._%+\-]+@(?:[A-Za-z0-9](?:[A-Za-z0-9\-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,}$";

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
                => UsersDomainErrors.Email.NullOrEmpty,

            { Length: > MaxLength }
                => UsersDomainErrors.Email.TooLong,

            _ when !EmailRegex.Value.IsMatch(email)
                => UsersDomainErrors.Email.InvalidFormat,

            _ => new Email { Value = email }
        };
}