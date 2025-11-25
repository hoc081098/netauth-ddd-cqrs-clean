using Quartz;

namespace NetAuth.Infrastructure.Outbox;

[DisallowConcurrentExecution]
internal sealed class OutboxMessagesProcessorJob(
    ILogger<OutboxMessagesProcessorJob> logger,
    IServiceScopeFactory scopeFactory
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            OutboxMessagesProcessorLoggers.LogStarting(logger);

            using var scope = scopeFactory.CreateScope();
            var outboxProcessor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

            await outboxProcessor.Execute();
        }
        catch (Exception ex)
        {
            OutboxMessagesProcessorLoggers.LogError(logger, ex);
            throw new JobExecutionException(ex, true);
        }
        finally
        {
            OutboxMessagesProcessorLoggers.LogFinished(logger);
        }
    }
}