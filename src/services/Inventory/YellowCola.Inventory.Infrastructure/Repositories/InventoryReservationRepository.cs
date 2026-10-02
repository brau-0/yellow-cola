using Microsoft.EntityFrameworkCore;
using YellowCola.Inventory.Application.Reservations;
using YellowCola.Inventory.Domain.Reservations;
using YellowCola.Inventory.Infrastructure.Persistence;

namespace YellowCola.Inventory.Infrastructure.Repositories;

internal sealed class InventoryReservationRepository(
    InventoryDbContext dbContext)
    : IInventoryReservationRepository
{
    public async Task AddAsync(
        InventoryReservation reservation,
        CancellationToken cancellationToken = default)
    {
        await dbContext.InventoryReservations
            .AddAsync(
                reservation,
                cancellationToken);
    }

    public Task<InventoryReservation?>
    GetForUpdateAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.InventoryReservations
            .FromSqlInterpolated(
                $"""
            SELECT
                id,
                order_id,
                inventory_item_id,
                quantity,
                status,
                created_at_utc,
                expires_at_utc
            FROM inventory_reservations
            WHERE id = {reservationId}
            FOR UPDATE
            """)
            .SingleOrDefaultAsync(
                cancellationToken);
    }
}