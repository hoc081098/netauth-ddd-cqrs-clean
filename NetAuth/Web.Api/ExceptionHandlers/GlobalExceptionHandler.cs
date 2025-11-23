using Microsoft.AspNetCore.Diagnostics;

namespace NetAuth.Web.Api.ExceptionHandlers;

/// <summary>
/// A global exception handler that catches all unhandled exceptions.
/// This handler runs last in the exception handler pipeline and provides a fallback
/// for any exceptions not caught by more specific handlers.
/// </summary>
/// <param name="problemDetailsService">Service to write standardized problem details.</param>
/// <param name="logger">Logger for recording exception details.</param>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        // Set status code of the response to 500 Internal Server Error
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Create standardized problem details for the 500 error
        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = "General.ServerError",
                Detail = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
            }
        };
        return await problemDetailsService.TryWriteAsync(problemDetailsContext);
    }
}