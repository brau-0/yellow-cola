using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace YellowCola.Orders.Infrastructure.Persistence.Outbox;

internal sealed partial class OutboxPublisherHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OutboxPublisherHostedService> logger) : BackgroundService
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The background publisher must remain alive when an iteration fails.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsEnabled())
        {
            LogPublisherDisabled(logger);
            return;
        }

        var batchSize = GetPositiveInt("OutboxPublisher:BatchSize", 20);
        var pollingIntervalMilliseconds = GetPositiveInt("OutboxPublisher:PollingIntervalMilliseconds", 1000);

        LogPublisherStarted(logger, batchSize, pollingIntervalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxPublisherProcessor>();
                var processed = await processor.PublishPendingAsync(batchSize, stoppingToken);

                if (processed > 0) LogBatchProcessed(logger, processed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogPublisherIterationFailed(logger, exception);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(pollingIntervalMilliseconds), stoppingToken);
        }
    }

    private bool IsEnabled() => !bool.TryParse(configuration["OutboxPublisher:Enabled"], out var enabled) || enabled;

    private int GetPositiveInt(string key, int fallback) => int.TryParse(configuration[key], out var value) && value > 0 ? value : fallback;

    [LoggerMessage(EventId = 1210, Level = LogLevel.Information, Message = "Outbox publisher started with batch size {BatchSize} and polling interval {PollingIntervalMilliseconds} ms.")]
    private static partial void LogPublisherStarted(ILogger logger, int batchSize, int pollingIntervalMilliseconds);

    [LoggerMessage(EventId = 1211, Level = LogLevel.Debug, Message = "Outbox publisher processed {MessageCount} message(s).")]
    private static partial void LogBatchProcessed(ILogger logger, int messageCount);

    [LoggerMessage(EventId = 1212, Level = LogLevel.Error, Message = "Outbox publisher iteration failed.")]
    private static partial void LogPublisherIterationFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1213, Level = LogLevel.Information, Message = "Outbox publisher is disabled.")]
    private static partial void LogPublisherDisabled(ILogger logger);
}