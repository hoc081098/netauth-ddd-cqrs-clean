using FluentValidation;
using FluentValidation.Results;
using LanguageExt;
using MediatR;
using NetAuth.Application.Core.Exceptions;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Core.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = await ValidateAsync(request);
        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        // If TResponse is Either<DomainError, T>, return a Left with ValidationError
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Either<,>) &&
            typeof(TResponse).GetGenericArguments()[0] == typeof(DomainError))
        {
            return ReturnValidationErrorAsLeft(failures);
        }

        throw new ValidationException(failures);
    }

    private async Task<List<ValidationFailure>> ValidateAsync(TRequest request)
    {
        var validationContext = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(validationContext)));

        return validationResults.Length == 0
            ? []
            : validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();
    }

    private static TResponse ReturnValidationErrorAsLeft(List<ValidationFailure> failures)
    {
        // Create closed generic type Either<DomainError, R>
        var closedEitherType = typeof(Either<,>)
            .MakeGenericType(typeof(DomainError), typeof(TResponse).GetGenericArguments()[1]);

        // use Reflection to call `public static method Either<DomainError, R>.Left(validationError)`
        var leftMethod = closedEitherType
            .GetMethod(
                name: nameof(Either<object, object>.Left),
                bindingAttr: System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                binder: null,
                types: [typeof(DomainError)],
                modifiers: null
            );

        var validationError = new ValidationError(failures);
        var leftValue = leftMethod!.Invoke(obj: null, parameters: [validationError])!;

        return (TResponse)leftValue;
    }
}