using Microsoft.AspNetCore.Diagnostics;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.ExceptionHandlers;

/// <summary>
/// An exception handler for handling <see cref="BadHttpRequestException"/>.
/// </summary>
/// <param name="problemDetailsService"></param>
/// <param name="logger"></param>
internal sealed class BadHttpRequestExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException validationException)
        {
            return false;
        }

        logger.LogError(validationException, "BadHttpRequestException occurred");

        // Set status code of the response to 400 Bad Request
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        // Create standardized problem details for the validation error
        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = validationException,
            ProblemDetails = CustomResults.GetProblemDetails(validationException),
        };
        return await problemDetailsService.TryWriteAsync(problemDetailsContext);
    }
}