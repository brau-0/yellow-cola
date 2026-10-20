using YellowCola.Checkout.Contracts.Messaging;

namespace YellowCola.Checkout.Contracts.Inventory;

public sealed record InventoryReservationResult(
    Guid ReservationId,
    Guid SkuId,
    int Quantity,
    DateTimeOffset ExpiresAtUtc);

public sealed record InventoryReservedEvent(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset CreatedAtUtc,
    Guid OrderId,
    IReadOnlyCollection<InventoryReservationResult> Reservations)
    : IntegrationMessage(MessageId, CorrelationId, CausationId, CreatedAtUtc)
{
    public override string MessageType => "checkout.inventory.inventory-reserved";
    public override int SchemaVersion => 1;
}