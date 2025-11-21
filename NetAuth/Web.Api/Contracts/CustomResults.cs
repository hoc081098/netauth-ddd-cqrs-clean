using System.Collections.Immutable;
using Ardalis.GuardClauses;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Web.Api.Contracts;

public static class CustomResults
{
    public static IResult Err(DomainError error) =>
        Err([error]);

    public static IResult Err(IReadOnlyList<DomainError> errors)
    {
        Guard.Against.NullOrEmpty(errors);

        var apiErrors = errors
            .Select(e =>
                new ApiError(
                    Code: e.Code,
                    Message: e.Message)
            )
            .ToArray();

        var response = new ApiErrorResponse(apiErrors);
        var statusCode = GetStatusCode(errors[0].Type);

        return Results.Json(response, statusCode: statusCode);
    }

    private static int GetStatusCode(DomainError.ErrorType type) =>
        type switch
        {
            DomainError.ErrorType.Failure => StatusCodes.Status500InternalServerError,
            DomainError.ErrorType.Validation => StatusCodes.Status400BadRequest,
            DomainError.ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            DomainError.ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            DomainError.ErrorType.NotFound => StatusCodes.Status404NotFound,
            DomainError.ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                message: "Unknown DomainError.ErrorType: " + type)
        };
}