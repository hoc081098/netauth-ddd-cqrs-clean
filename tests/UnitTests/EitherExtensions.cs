using LanguageExt;
using NetAuth.Domain.Core.Primitives;
using static LanguageExt.Prelude;

namespace NetAuth.UnitTests;

public static class EitherExtensions
{
    public static T RightValueOrThrow<T>(this Either<DomainError, T> either)
    {
        return either.Match(Right: identity,
            Left: error =>
            {
                Assert.Fail("Expected a Right value but got Left with error: " + error);
                throw new InvalidOperationException("Unreachable code");
            });
    }
}