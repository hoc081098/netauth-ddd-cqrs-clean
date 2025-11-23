using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.ExceptionHandler;

/// <summary>
/// An exception handler for handling FluentValidation's <see cref="ValidationException"/>.
/// </summary>
/// <param name="problemDetailsService"></param>
/// <param name="logger"></param>
internal sealed class ValidationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        logger.LogError(validationException, "An unhandled exception occurred");

        // Set status code of the response to 400 Bad Request
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = validationException,
            ProblemDetails = CustomResults.GetProblemDetails(validationException),
        };
        return await problemDetailsService.TryWriteAsync(problemDetailsContext);
    }
}