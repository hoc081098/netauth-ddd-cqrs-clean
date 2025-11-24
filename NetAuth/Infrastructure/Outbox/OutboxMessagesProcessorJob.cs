using Microsoft.Extensions.Options;
using Quartz;

namespace NetAuth.Infrastructure.Outbox;

[DisallowConcurrentExecution]
internal sealed class OutboxMessagesProcessorJob(
    ILogger<OutboxMessagesProcessorJob> logger,
    IOptions<OutboxSettings> outboxSettingsOptions) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Outbox Messages Processor Job Starting at {DateTime}", DateTimeOffset.Now);

        await Task.Delay(15_000); // Simulate some work

        logger.LogInformation("Outbox Messages Processor Job Completed at {DateTime}", DateTimeOffset.Now);
    }
}