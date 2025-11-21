using FluentValidation;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Core.Extensions;

/// <summary>
/// Contains extension methods for fluent validations.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Specifies a custom error to use if validation fails.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property being validated.</typeparam>
    /// <param name="rule">The current rule.</param>
    /// <param name="error">The error to use.</param>
    /// <returns>The same rule builder.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithDomainError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        DomainError error) =>
        rule.WithErrorCode(error.Code)
            .WithMessage(error.Message);
}