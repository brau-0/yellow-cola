using YellowCola.Inventory.Domain.Reservations;

namespace YellowCola.Inventory.Application.Inventory;

public sealed record ReserveInventoryResult(
    Guid ReservationId,
    Guid OrderId,
    Guid InventoryItemId,
    Guid SkuId,
    string WarehouseCode,
    int Quantity,
    InventoryReservationStatus Status,
    DateTimeOffset ExpiresAtUtc,
    int OnHand,
    int Reserved,
    int Available);