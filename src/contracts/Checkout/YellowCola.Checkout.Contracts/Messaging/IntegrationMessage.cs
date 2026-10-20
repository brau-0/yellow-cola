namespace YellowCola.Checkout.Contracts.Messaging;

public abstract record IntegrationMessage(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset CreatedAtUtc)
{
    public abstract string MessageType { get; }
    public abstract int SchemaVersion { get; }
}