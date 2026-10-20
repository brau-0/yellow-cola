using System.Text.Json;

using YellowCola.Checkout.Contracts.Messaging;

namespace YellowCola.Inventory.Infrastructure.Persistence.Messaging;

internal sealed class InventoryOutboxMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private InventoryOutboxMessage()
    {
    }

    private InventoryOutboxMessage(Guid messageId, string messageType, int schemaVersion, Guid correlationId, Guid? causationId, string payload, DateTimeOffset createdAtUtc)
    {
        MessageId = messageId;
        MessageType = messageType;
        SchemaVersion = schemaVersion;
        CorrelationId = correlationId;
        CausationId = causationId;
        Payload = payload;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid MessageId { get; private set; }
    public string MessageType { get; private set; } = string.Empty;
    public int SchemaVersion { get; private set; }
    public Guid CorrelationId { get; private set; }
    public Guid? CausationId { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public int PublishAttempts { get; private set; }
    public string? LastError { get; private set; }

    public static InventoryOutboxMessage From(IntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var payload = JsonSerializer.Serialize(message, message.GetType(), JsonOptions);

        return new InventoryOutboxMessage(message.MessageId, message.MessageType, message.SchemaVersion, message.CorrelationId, message.CausationId, payload, message.CreatedAtUtc);
    }
}