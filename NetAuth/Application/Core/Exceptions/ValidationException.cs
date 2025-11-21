using FluentValidation.Results;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Core.Exceptions;

public sealed class ValidationException(
    IEnumerable<ValidationFailure> failures
) : Exception
{
    public IReadOnlyList<DomainError> Errors { get; } =
        failures
            .Select(failure => new DomainError(failure.ErrorCode, failure.ErrorMessage))
            .Distinct()
            .ToList();
}