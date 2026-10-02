using YellowCola.Inventory.Domain.Reservations;

namespace YellowCola.Inventory.Application.Reservations;

public interface IInventoryReservationRepository
{
    Task<InventoryReservation?> GetForUpdateAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        InventoryReservation reservation,
        CancellationToken cancellationToken = default);
}