using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using LanguageExt;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.Users;

public sealed class Username : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 50;

    private const string UsernameRegexPattern = @"^[a-zA-Z0-9_\-]+$";

    private static readonly Lazy<Regex> UsernameRegex = new(() =>
        new(UsernameRegexPattern, RegexOptions.Compiled));

    public required string Value { get; init; }

    private Username()
    {
    }

    protected override IEnumerable<object> GetAtomicValues() => [Value];

    public static implicit operator string(Username username) => username.Value;

    [Pure]
    public static Either<DomainError, Username> Create(string username) =>
        username switch
        {
            _ when string.IsNullOrWhiteSpace(username)
                => UsersDomainErrors.Username.NullOrEmpty,

            { Length: < MinLength }
                => UsersDomainErrors.Username.TooShort,

            { Length: > MaxLength }
                => UsersDomainErrors.Username.TooLong,

            _ when !UsernameRegex.Value.IsMatch(username)
                => UsersDomainErrors.Username.InvalidFormat,

            _ => new Username { Value = username }
        };
}