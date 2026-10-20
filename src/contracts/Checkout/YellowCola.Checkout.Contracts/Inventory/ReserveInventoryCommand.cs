using YellowCola.Checkout.Contracts.Messaging;

namespace YellowCola.Checkout.Contracts.Inventory;

public sealed record ReserveInventoryLine(Guid SkuId, int Quantity);

public sealed record ReserveInventoryCommand(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset CreatedAtUtc,
    Guid OrderId,
    string WarehouseCode,
    IReadOnlyCollection<ReserveInventoryLine> Lines)
    : IntegrationMessage(MessageId, CorrelationId, CausationId, CreatedAtUtc)
{
    public override string MessageType => "checkout.inventory.reserve-inventory";
    public override int SchemaVersion => 1;
}