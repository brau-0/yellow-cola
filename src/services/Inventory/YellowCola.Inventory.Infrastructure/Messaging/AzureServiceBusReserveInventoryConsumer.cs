using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using YellowCola.Inventory.Application.Messaging;

using IntegrationReserveInventoryCommand = YellowCola.Checkout.Contracts.Inventory.ReserveInventoryCommand;

namespace YellowCola.Inventory.Infrastructure.Messaging;

internal sealed partial class AzureServiceBusReserveInventoryConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AzureServiceBusReserveInventoryConsumer> logger) : BackgroundService
{
    private const string ReserveInventoryMessageType = "checkout.inventory.reserve-inventory";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var transport = configuration["Messaging:Transport"] ?? "Local";

        if (!string.Equals(transport, "AzureServiceBus", StringComparison.Ordinal))
        {
            LogConsumerDisabled(logger);
            return;
        }

        var connectionString = configuration["Messaging:ServiceBus:ConnectionString"] ?? throw new InvalidOperationException("Service Bus connection string was not configured.");
        var queueName = configuration["Messaging:ServiceBus:InventoryCommandsQueue"] ?? throw new InvalidOperationException("Inventory commands queue was not configured.");
        var maxConcurrentCalls = GetPositiveInt(configuration["Messaging:ServiceBus:MaxConcurrentCalls"], 1);

        await using var client = new ServiceBusClient(connectionString);

        await using var processor = client.CreateProcessor(
            queueName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = maxConcurrentCalls,
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            });

        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;

        LogConsumerStarted(logger, queueName, maxConcurrentCalls);

        await processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        if (!string.Equals(args.Message.Subject, ReserveInventoryMessageType, StringComparison.Ordinal))
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "UnsupportedMessageType",
                $"Message type '{args.Message.Subject}' is not supported by the Inventory command consumer.",
                args.CancellationToken);

            return;
        }

        IntegrationReserveInventoryCommand? command;

        try
        {
            command = JsonSerializer.Deserialize<IntegrationReserveInventoryCommand>(args.Message.Body.ToString(), JsonOptions);
        }
        catch (JsonException exception)
        {
            LogInvalidMessage(logger, exception, args.Message.MessageId);

            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidPayload",
                "The ReserveInventoryCommand payload could not be deserialized.",
                args.CancellationToken);

            return;
        }

        if (command is null)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidPayload",
                "The ReserveInventoryCommand payload was null.",
                args.CancellationToken);

            return;
        }

        if (command.SchemaVersion != 1)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "UnsupportedSchemaVersion",
                $"Schema version '{command.SchemaVersion}' is not supported.",
                args.CancellationToken);

            return;
        }

        if (!string.Equals(args.Message.MessageId, command.MessageId.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "MessageIdMismatch",
                "Service Bus MessageId does not match the payload MessageId.",
                args.CancellationToken);

            return;
        }

        if (!string.Equals(args.Message.CorrelationId, command.CorrelationId.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "CorrelationIdMismatch",
                "Service Bus CorrelationId does not match the payload CorrelationId.",
                args.CancellationToken);

            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<ReserveInventoryMessageHandler>();
        var outcome = await handler.HandleAsync(command, args.CancellationToken);

        await args.CompleteMessageAsync(args.Message, args.CancellationToken);

        LogMessageCompleted(logger, args.Message.MessageId, outcome);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        LogProcessorError(logger, args.Exception, args.ErrorSource.ToString(), args.EntityPath);
        return Task.CompletedTask;
    }

    private static int GetPositiveInt(string? value, int fallback) => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;

    [LoggerMessage(EventId = 2201, Level = LogLevel.Information, Message = "Inventory Service Bus consumer started for queue {QueueName} with MaxConcurrentCalls {MaxConcurrentCalls}.")]
    private static partial void LogConsumerStarted(ILogger logger, string queueName, int maxConcurrentCalls);
    [LoggerMessage(EventId = 2202, Level = LogLevel.Information, Message = "Inventory processed Service Bus message {MessageId} with outcome {Outcome}.")]
    private static partial void LogMessageCompleted(ILogger logger, string messageId, InventoryMessageHandlingOutcome outcome);

    [LoggerMessage(EventId = 2203, Level = LogLevel.Warning, Message = "Inventory received invalid payload for Service Bus message {MessageId}.")]
    private static partial void LogInvalidMessage(ILogger logger, Exception exception, string messageId);

    [LoggerMessage(EventId = 2204, Level = LogLevel.Error, Message = "Inventory Service Bus processor error from source {ErrorSource} on entity {EntityPath}.")]
    private static partial void LogProcessorError(ILogger logger, Exception exception, string errorSource, string entityPath);

    [LoggerMessage(EventId = 2205, Level = LogLevel.Information, Message = "Inventory Service Bus consumer is disabled because the configured transport is not AzureServiceBus.")]
    private static partial void LogConsumerDisabled(ILogger logger);
}