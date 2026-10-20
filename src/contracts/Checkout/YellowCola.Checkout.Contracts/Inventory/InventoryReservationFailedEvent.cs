using YellowCola.Checkout.Contracts.Messaging;

namespace YellowCola.Checkout.Contracts.Inventory;

public sealed record InventoryReservationFailedEvent(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset CreatedAtUtc,
    Guid OrderId,
    string FailureCode,
    string Reason)
    : IntegrationMessage(MessageId, CorrelationId, CausationId, CreatedAtUtc)
{
    public override string MessageType => "checkout.inventory.inventory-reservation-failed";
    public override int SchemaVersion => 1;
}