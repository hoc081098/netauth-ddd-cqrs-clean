using System.Collections.Concurrent;
using System.Reflection;
using Ardalis.GuardClauses;
using FluentValidation;
using FluentValidation.Results;
using LanguageExt;
using MediatR;
using NetAuth.Application.Core.Errors;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Core.Behaviors;

internal static class EitherLeftMethodCache
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> LeftMethodInfosCache = new();

    private static MethodInfo GetMethod(Type rightType)
    {
        // Create closed generic type Either<DomainError, R>
        var closedEitherType = typeof(Either<,>)
            .MakeGenericType(typeof(DomainError), rightType);

        // use Reflection to call `public static method Either<DomainError, R>.Left(validationError)`
        var leftMethod = closedEitherType
            .GetMethod(
                name: nameof(Either<,>.Left),
                bindingAttr: BindingFlags.Static | BindingFlags.Public,
                binder: null,
                types: [typeof(DomainError)],
                modifiers: null
            );

        return Guard.Against.Null(leftMethod,
            exceptionCreator: () => new InvalidOperationException("Could not find Left method on Either type."));
    }

    internal static MethodInfo GetOrAdd(Type rightType) => LeftMethodInfosCache.GetOrAdd(rightType, GetMethod);
}

internal sealed class ValidationPipelineBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = await ValidateAsync(request, cancellationToken);
        return failures switch
        {
            { Count: 0 } => await next(cancellationToken),
            _ when DomainErrorEitherTypeChecker.IsDomainErrorEither<TResponse>(out var rightType) =>
                CreateValidationErrorLeft(failures, rightType),
            _ => throw new ValidationException(failures)
        };
    }

    private async Task<IReadOnlyList<ValidationFailure>> ValidateAsync(TRequest request,
        CancellationToken cancellationToken)
    {
        var validationContext = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(v =>
            v.ValidateAsync(validationContext, cancellationToken)));

        return validationResults.Length == 0
            ? []
            : validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToArray();
    }


    private static TResponse CreateValidationErrorLeft(IReadOnlyList<ValidationFailure> failures, Type rightType)
    {
        var leftMethod = EitherLeftMethodCache.GetOrAdd(rightType);

        var validationError = new ValidationError(failures);
        var leftValue = leftMethod.Invoke(obj: null, parameters: [validationError])!;

        return (TResponse)leftValue;
    }
}