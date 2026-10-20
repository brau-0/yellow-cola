using Microsoft.Extensions.Logging;

using YellowCola.Orders.Application.Messaging;

namespace YellowCola.Orders.Infrastructure.Messaging;

internal sealed partial class LocalLoggingIntegrationMessageTransport(ILogger<LocalLoggingIntegrationMessageTransport> logger) : IIntegrationMessageTransport
{
    public Task SendAsync(OutboundMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        LogMessageAccepted(logger, message.MessageId, message.MessageType, message.CorrelationId, message.SchemaVersion);

        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Local transport accepted message {MessageId} of type {MessageType}, correlation {CorrelationId}, schema version {SchemaVersion}.")]
    private static partial void LogMessageAccepted(ILogger logger, Guid messageId, string messageType, Guid correlationId, int schemaVersion);
}