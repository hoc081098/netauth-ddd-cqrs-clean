using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NetAuth.Application.Core.Exceptions;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Web.Api.Contracts;

public static class CustomResults
{
    public static IResult Err(DomainError error)
    {
        Guard.Against.Null(error);
        return Results.Problem(GetProblemDetails(error));
    }

    internal static ProblemDetails GetProblemDetails(ValidationException exception) =>
        GetProblemDetails(new ValidationError(exception.Errors));

    private static ProblemDetails GetProblemDetails(DomainError error)
    {
        var details = new ProblemDetails
        {
            Title = GetTitle(error),
            Detail = GetDetail(error),
            Status = GetStatusCode(error.Type),
            Type = GetType(error.Type),
        };
        if (GetErrors(error) is { } errors)
        {
            details.Extensions = errors;
        }

        return details;


        static string GetTitle(DomainError error) =>
            error.Type switch
            {
                DomainError.ErrorType.Validation => error.Code,
                DomainError.ErrorType.Unauthorized => error.Code,
                DomainError.ErrorType.Forbidden => error.Code,
                DomainError.ErrorType.NotFound => error.Code,
                DomainError.ErrorType.Conflict => error.Code,
                DomainError.ErrorType.Failure => error.Code,
                _ => "General.ServerError"
            };

        static string GetDetail(DomainError error) =>
            error.Type switch
            {
                DomainError.ErrorType.Validation => error.Message,
                DomainError.ErrorType.Unauthorized => error.Message,
                DomainError.ErrorType.Forbidden => error.Message,
                DomainError.ErrorType.NotFound => error.Message,
                DomainError.ErrorType.Conflict => error.Message,
                DomainError.ErrorType.Failure => error.Message,
                _ => "An unexpected error occurred."
            };

        static int GetStatusCode(DomainError.ErrorType type) =>
            type switch
            {
                DomainError.ErrorType.Validation => StatusCodes.Status400BadRequest,
                DomainError.ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                DomainError.ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                DomainError.ErrorType.NotFound => StatusCodes.Status404NotFound,
                DomainError.ErrorType.Conflict => StatusCodes.Status409Conflict,
                DomainError.ErrorType.Failure => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            };

        static string GetType(DomainError.ErrorType type)
        {
            // Return a link to the relevant RFC section for the error type
            return type switch
            {
                DomainError.ErrorType.Validation => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                DomainError.ErrorType.Unauthorized => "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
                DomainError.ErrorType.Forbidden => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
                DomainError.ErrorType.NotFound => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                DomainError.ErrorType.Conflict => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
                DomainError.ErrorType.Failure => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                _ => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
            };
        }

        static Dictionary<string, object?>? GetErrors(DomainError error)
        {
            return error switch
            {
                ValidationError { Errors: { Count: > 0 } errors } =>
                    new Dictionary<string, object?>
                    {
                        {
                            "errors",
                            errors
                                .Select(e => new ApiErrorResponse(
                                        Code: e.Code,
                                        Message: e.Message,
                                        ErrorType: GetErrorType(e)
                                    )
                                )
                                .ToArray()
                        }
                    },
                _ => null
            };

            static string GetErrorType(DomainError e) =>
                e.Type switch
                {
                    DomainError.ErrorType.Validation => "Validation",
                    DomainError.ErrorType.NotFound => "NotFound",
                    DomainError.ErrorType.Conflict => "Conflict",
                    DomainError.ErrorType.Unauthorized => "Unauthorized",
                    DomainError.ErrorType.Forbidden => "Forbidden",
                    DomainError.ErrorType.Failure => "Failure",
                    _ => "InternalServerError"
                };
        }
    }
}