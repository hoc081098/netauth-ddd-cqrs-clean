using Ardalis.GuardClauses;
using FluentValidation.Results;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Core.Errors;

public sealed class ValidationError(
    IEnumerable<ValidationFailure> failures
) : DomainError(
    code: "General.ValidationError",
    message: "One or more validation errors occurred.",
    type: ErrorType.Validation)
{
    public IReadOnlyList<DomainError> Errors { get; } =
        Guard.Against.NullOrEmpty(failures)
            .Select(failure =>
                new DomainError(code: failure.ErrorCode,
                    message: failure.ErrorMessage,
                    type: ErrorType.Validation))
            .Distinct()
            .ToArray();
}