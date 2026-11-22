using System.Diagnostics.CodeAnalysis;
using LanguageExt;
using MediatR;
using Serilog.Context;
using Unit = MediatR.Unit;

namespace NetAuth.Application.Core.Behaviors;

internal sealed partial class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    [SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both")]
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        LogProcessing(requestName);
        try
        {
            var result = await next(cancellationToken);
            if (DomainErrorEitherTypeChecker.IsDomainErrorEither<TResponse>(out var _) && result is IEither either)
            {
                either.MatchUntyped(
                    Right: _ =>
                    {
                        LogCompletedRequest(requestName);
                        return Unit.Value;
                    },
                    Left: error =>
                    {
                        LogCompletedRequestDomainError(requestName, error);
                        return Unit.Value;
                    }
                );
            }
            else
            {
                LogCompletedRequest(requestName);
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
                LogCompletedRequestWithError(requestName, exception);
            }

            throw;
        }
    }

    [LoggerMessage(LogLevel.Information, "[PipelineBehavior] Processing request {RequestName}")]
    partial void LogProcessing(string requestName);

    [LoggerMessage(LogLevel.Information, "[PipelineBehavior] Completed request {RequestName}")]
    partial void LogCompletedRequest(string requestName);

    [LoggerMessage(LogLevel.Information, "[PipelineBehavior] Completed request {RequestName} with domain error: {@DomainError}")]
    partial void LogCompletedRequestDomainError(string requestName, object domainError);

    [LoggerMessage(LogLevel.Error, "[PipelineBehavior] Completed request {RequestName} with error")]
    partial void LogCompletedRequestWithError(string requestName, Exception exception);
}