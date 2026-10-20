using System.Text.Json;

using YellowCola.Checkout.Contracts.Messaging;
using YellowCola.Orders.Application.Messaging;

namespace YellowCola.Orders.Infrastructure.Persistence.Outbox;

internal sealed class OutboxMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private OutboxMessage()
    {
    }

    private OutboxMessage(Guid messageId, string messageType, int schemaVersion, Guid correlationId, Guid? causationId, string payload, DateTimeOffset createdAtUtc)
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

    public static OutboxMessage From(IntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.MessageId == Guid.Empty) throw new ArgumentException("Message id cannot be empty.", nameof(message));
        if (message.CorrelationId == Guid.Empty) throw new ArgumentException("Correlation id cannot be empty.", nameof(message));
        if (message.CreatedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("CreatedAtUtc must be UTC.", nameof(message));
        if (string.IsNullOrWhiteSpace(message.MessageType)) throw new ArgumentException("Message type is required.", nameof(message));
        if (message.SchemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(message), "Schema version must be greater than zero.");

        var payload = JsonSerializer.Serialize(message, message.GetType(), JsonOptions);

        return new OutboxMessage(message.MessageId, message.MessageType, message.SchemaVersion, message.CorrelationId, message.CausationId, payload, message.CreatedAtUtc);
    }
    public OutboundMessage ToOutboundMessage() => new(MessageId, MessageType, SchemaVersion, CorrelationId, CausationId, CreatedAtUtc, Payload);

    public void MarkPublished(DateTimeOffset publishedAtUtc)
    {
        if (publishedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("PublishedAtUtc must be UTC.", nameof(publishedAtUtc));
        if (PublishedAtUtc.HasValue) return;

        PublishAttempts = checked(PublishAttempts + 1);
        PublishedAtUtc = publishedAtUtc;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        if (string.IsNullOrWhiteSpace(error)) throw new ArgumentException("Error is required.", nameof(error));

        var normalizedError = error.Trim();

        PublishAttempts = checked(PublishAttempts + 1);
        LastError = normalizedError.Length <= 2000 ? normalizedError : normalizedError[..2000];
    }
}