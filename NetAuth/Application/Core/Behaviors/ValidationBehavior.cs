using System.Collections.Concurrent;
using System.Reflection;
using Ardalis.GuardClauses;
using FluentValidation;
using FluentValidation.Results;
using LanguageExt;
using MediatR;
using NetAuth.Application.Core.Exceptions;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Core.Behaviors;

internal static class EitherLeftMethodCache
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> _leftMethodInfosCache = new();

    private static MethodInfo GetMethod(Type rightType)
    {
        // Create closed generic type Either<DomainError, R>
        var closedEitherType = typeof(Either<,>)
            .MakeGenericType(typeof(DomainError), rightType);

        // use Reflection to call `public static method Either<DomainError, R>.Left(validationError)`
        var leftMethod = closedEitherType
            .GetMethod(
                name: nameof(Either<object, object>.Left),
                bindingAttr: BindingFlags.Static | BindingFlags.Public,
                binder: null,
                types: [typeof(DomainError)],
                modifiers: null
            );

        return Guard.Against.Null(leftMethod,
            exceptionCreator: () => new InvalidOperationException("Could not find Left method on Either type."));
    }

    internal static MethodInfo GetOrAdd(Type rightType) => _leftMethodInfosCache.GetOrAdd(rightType, GetMethod);
}

internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = await ValidateAsync(request, cancellationToken);
        return failures switch
        {
            { Count: 0 } => await next(cancellationToken),
            _ when IsEitherDomainError(out var rightType) => ReturnValidationErrorAsLeft(failures, rightType),
            _ => throw new ValidationException(failures)
        };
    }

    private async Task<List<ValidationFailure>> ValidateAsync(TRequest request, CancellationToken cancellationToken)
    {
        var validationContext = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(v =>
            v.ValidateAsync(validationContext, cancellationToken)));

        return validationResults.Length == 0
            ? []
            : validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();
    }

    private static bool IsEitherDomainError(out Type rightType)
    {
        if (!typeof(TResponse).IsGenericType)
        {
            rightType = typeof(void);
            return false;
        }

        if (typeof(TResponse).GetGenericTypeDefinition() == typeof(Either<,>))
        {
            var genericArguments = typeof(TResponse).GetGenericArguments();
            if (genericArguments[0] == typeof(DomainError))
            {
                rightType = genericArguments[1];
                return true;
            }
        }

        rightType = typeof(void);
        return false;
    }

    private static TResponse ReturnValidationErrorAsLeft(List<ValidationFailure> failures, Type rightType)
    {
        var leftMethod = EitherLeftMethodCache.GetOrAdd(rightType);

        var validationError = new ValidationError(failures);
        var leftValue = leftMethod.Invoke(obj: null, parameters: [validationError])!;

        return (TResponse)leftValue;
    }
}