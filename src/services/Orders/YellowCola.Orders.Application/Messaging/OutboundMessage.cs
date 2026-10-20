namespace YellowCola.Orders.Application.Messaging;

public sealed record OutboundMessage(
    Guid MessageId,
    string MessageType,
    int SchemaVersion,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset CreatedAtUtc,
    string Payload);

