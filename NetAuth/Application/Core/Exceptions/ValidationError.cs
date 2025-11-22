using FluentValidation.Results;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Core.Exceptions;

public sealed class ValidationError(
    IEnumerable<ValidationFailure> failures
) : DomainError(
    code: "General.ValidationError",
    message: "One or more validation errors occurred.",
    type: ErrorType.Validation)
{
    public IReadOnlyList<DomainError> Errors { get; } =
        failures
            .Select(failure =>
                new DomainError(code: failure.ErrorCode,
                    message: failure.ErrorMessage,
                    type: DomainError.ErrorType.Validation))
            .Distinct()
            .ToArray();
}