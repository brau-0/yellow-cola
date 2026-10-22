using Azure.Messaging.ServiceBus;

using YellowCola.Orders.Application.Messaging;

namespace YellowCola.Orders.Infrastructure.Messaging;

internal sealed class AzureServiceBusIntegrationMessageTransport(ServiceBusSender sender) : IIntegrationMessageTransport
{
    public async Task SendAsync(OutboundMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromString(message.Payload))
        {
            MessageId = message.MessageId.ToString(),
            CorrelationId = message.CorrelationId.ToString(),
            Subject = message.MessageType,
            ContentType = "application/json"
        };

        serviceBusMessage.ApplicationProperties["messageType"] = message.MessageType;
        serviceBusMessage.ApplicationProperties["schemaVersion"] = message.SchemaVersion;

        if (message.CausationId.HasValue) serviceBusMessage.ApplicationProperties["causationId"] = message.CausationId.Value.ToString();

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}