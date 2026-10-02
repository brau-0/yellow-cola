using YellowCola.Inventory.Domain.Reservations;

namespace YellowCola.Inventory.Application.Reservations;

public sealed record ReservationTransitionResult(
    Guid ReservationId,
    Guid OrderId,
    Guid InventoryItemId,
    int Quantity,
    InventoryReservationStatus Status,
    int OnHand,
    int Reserved,
    int Available);