namespace YellowCola.Inventory.Application.Inventory;

public sealed record ReserveInventoryResult(
    Guid InventoryItemId,
    Guid SkuId,
    string WarehouseCode,
    int OnHand,
    int Reserved,
    int Available);