using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NetAuth.Infrastructure.Outbox;

namespace NetAuth.Infrastructure.HealthChecks;

internal sealed class OutboxHealthCheck(
    AppDbContext dbContext,
    IOptions<OutboxSettings> outboxSettingsOptions
) : IHealthCheck
{
    private const int MaxFailedToPublishThreshold = 100;
    private const int UnprocessedMessageWarningMultiplier = 10;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var unprocessedCount = await dbContext
            .OutboxMessages
            .CountAsync(om => om.ProcessedOnUtc == null, cancellationToken);

        var failedToPublishCount = await dbContext
            .OutboxMessages
            .CountAsync(om => om.Error != null && om.ProcessedOnUtc == null, cancellationToken);

        var healthCheckData = new Dictionary<string, object>()
        {
            { "unprocessed_count", unprocessedCount },
            { "failed_to_publish_count", failedToPublishCount }
        };
        var outboxSettings = outboxSettingsOptions.Value;
        var batchSize = outboxSettings.BatchSize;

        var metrics = (unprocessedCount, failedToPublishCount);
        return metrics switch
        {
            (_, > MaxFailedToPublishThreshold) =>
                HealthCheckResult.Degraded(
                    description: $"There are {failedToPublishCount} messages failed to publish in outbox.",
                    data: healthCheckData),

            var (unprocessed, _) when unprocessed > batchSize * UnprocessedMessageWarningMultiplier =>
                HealthCheckResult.Unhealthy(
                    description: $"Outbox backlog too large: {unprocessedCount} messages.",
                    data: healthCheckData),

            _ => HealthCheckResult.Healthy("Outbox processor is healthy", data: healthCheckData),
        };
    }
}