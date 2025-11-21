using FluentValidation;
using MediatR;

namespace NetAuth.Application.Core.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return next(cancellationToken);

        var validationContext = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(validationContext))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        return failures.Count != 0
            ? throw new NetAuth.Application.Core.Exceptions.ValidationException(failures)
            : next(cancellationToken);
    }
}