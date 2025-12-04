using LanguageExt;
using MediatR;
using Serilog.Context;
using Unit = MediatR.Unit;

namespace NetAuth.Application.Core.Behaviors;

internal sealed class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Processing request {RequestName}", requestName);
        try
        {
            var result = await next(cancellationToken);
            if (DomainErrorEitherTypeChecker.IsDomainErrorEither<TResponse>(out var _) && result is IEither either)
            {
                either.MatchUntyped(
                    Right: _ =>
                    {
                        logger.LogInformation("Completed request {RequestName}", requestName);
                        return Unit.Value;
                    },
                    Left: error =>
                    {
                        logger.LogInformation(
                            "Completed request {RequestName} with domain error: {@DomainError}",
                            requestName,
                            error);
                        return Unit.Value;
                    }
                );
            }
            else
            {
                logger.LogInformation("Completed request {RequestName}", requestName);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            using (LogContext.PushProperty("Error", exception, true))
            {
                logger.LogError(exception, "Completed request {RequestName} with error", requestName);
            }

            throw;
        }
    }
}