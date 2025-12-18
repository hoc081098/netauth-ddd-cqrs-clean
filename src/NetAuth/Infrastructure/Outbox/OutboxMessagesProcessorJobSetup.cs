using Microsoft.Extensions.Options;
using Quartz;

namespace NetAuth.Infrastructure.Outbox;

internal sealed class OutboxMessagesProcessorJobSetup(
    IOptions<OutboxSettings> outboxSettingsOptions) : IConfigureOptions<QuartzOptions>
{
    private const string CleanupJobTriggerIdentity = "Outbox.Cleanup.Trigger";
    private static readonly JobKey CleanupJobKey = new(nameof(OutboxCleanupJob));

    private const string ProcessJobTriggerIdentity = "Outbox.Process.Trigger";
    private static readonly JobKey ProcessJobKey = new(nameof(OutboxMessagesProcessorJob));

    public void Configure(QuartzOptions options)
    {
        var outboxSettings = outboxSettingsOptions.Value;

        options.AddJob<OutboxMessagesProcessorJob>(jobBuilder => jobBuilder.WithIdentity(ProcessJobKey))
            .AddTrigger(trigger =>
                trigger
                    .WithIdentity(ProcessJobTriggerIdentity)
                    .ForJob(ProcessJobKey)
                    .WithSimpleSchedule(scheduleBuilder =>
                        scheduleBuilder
                            .WithInterval(outboxSettings.Interval)
                            .RepeatForever()));

        // Cleanup job - daily at 03:00 (can be overridden later if needed)
        options.AddJob<OutboxCleanupJob>(jobBuilder => jobBuilder.WithIdentity(CleanupJobKey))
            .AddTrigger(trigger =>
                trigger
                    .WithIdentity(CleanupJobTriggerIdentity)
                    .ForJob(CleanupJobKey)
                    .WithCronSchedule("0 0 3 * * ?"));

        // 0 0 3 * * ? (Quartz cron) triggers at 03:00:00 every day. Breakdown of the fields:
        // Seconds: 0
        // Minutes: 0
        // Hours: 3
        // Day-of-month: * (every day)
        // Month: * (every month)
        // Day-of-week: ? (no specific value; used as a placeholder because day-of-month is used)
        // Year: omitted (every year)
        // So the job runs daily at 3:00 AM (Quartz uses the scheduler's configured timezone by default).
    }
}