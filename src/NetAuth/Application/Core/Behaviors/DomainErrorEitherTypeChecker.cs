using LanguageExt;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Core.Behaviors;

internal static class DomainErrorEitherTypeChecker
{
    internal static bool IsDomainErrorEither<TResponse>(out Type rightType)
    {
        if (!typeof(TResponse).IsGenericType)
        {
            rightType = typeof(void);
            return false;
        }

        if (typeof(TResponse).GetGenericTypeDefinition() == typeof(Either<,>))
        {
            var genericArguments = typeof(TResponse).GetGenericArguments();
            // Only accept Either<DomainError, R>
            if (genericArguments[0] == typeof(DomainError))
            {
                rightType = genericArguments[1];
                return true;
            }
        }

        rightType = typeof(void);
        return false;
    }
}