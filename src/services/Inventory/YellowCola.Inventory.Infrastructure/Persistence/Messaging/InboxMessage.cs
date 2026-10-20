namespace YellowCola.Inventory.Infrastructure.Persistence.Messaging;

internal sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    public Guid MessageId { get; private set; }
    public string MessageType { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
}