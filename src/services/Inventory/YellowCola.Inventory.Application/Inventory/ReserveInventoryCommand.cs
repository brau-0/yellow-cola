namespace YellowCola.Inventory.Application.Inventory;

public sealed record ReserveInventoryCommand(
    Guid OrderId,
    Guid SkuId,
    string WarehouseCode,
    int Quantity);