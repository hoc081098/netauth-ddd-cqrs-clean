using Microsoft.Extensions.Options;
using Quartz;

namespace NetAuth.Infrastructure.Outbox;

internal sealed class OutboxMessagesProcessorJobSetup(
    IOptions<OutboxSettings> outboxSettingsOptions) : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var outboxSettings = outboxSettingsOptions.Value;

        var jobKey = new JobKey(nameof(OutboxMessagesProcessorJob));

        options.AddJob<OutboxMessagesProcessorJob>(jobBuilder => jobBuilder.WithIdentity(jobKey))
            .AddTrigger(trigger =>
                trigger.ForJob(jobKey)
                    .WithSimpleSchedule(scheduleBuilder =>
                        scheduleBuilder
                            .WithInterval(outboxSettings.Interval)
                            .RepeatForever()));
    }
}