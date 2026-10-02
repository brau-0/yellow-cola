using YellowCola.Inventory.Application.Persistence;
using YellowCola.Inventory.Application.Reservations;
using YellowCola.Inventory.Domain.InventoryItems;
using YellowCola.Inventory.Domain.Reservations;

namespace YellowCola.Inventory.Application.Inventory;

public sealed class InventoryApplicationService(
    IInventoryRepository inventoryRepository,
    IInventoryReservationRepository reservationRepository,
    IInventoryUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan
        ReservationLifetime =
            TimeSpan.FromMinutes(15);

    public async Task<ReserveInventoryResult?>
        ReserveAsync(
            ReserveInventoryCommand command,
            CancellationToken cancellationToken = default)
    {
        if (command.OrderId == Guid.Empty)
        {
            throw new ArgumentException("Order id cannot be empty.", nameof(command));
        }

        if (command.SkuId == Guid.Empty)
        {
            throw new ArgumentException("SKU id cannot be empty.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.WarehouseCode))
        {
            throw new ArgumentException("Warehouse code is required.", nameof(command));
        }

        if (command.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException( nameof(command), "Quantity must be greater than zero.");
        }

        var warehouseCode = command.WarehouseCode.Trim();

        await using var transaction = await unitOfWork .BeginTransactionAsync(cancellationToken);

        try
        {
            var inventoryItem = await inventoryRepository.GetForUpdateAsync(command.SkuId, warehouseCode, cancellationToken);

            if (inventoryItem is null)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                return null;
            }

            inventoryItem.Reserve(command.Quantity);

            var createdAtUtc =timeProvider.GetUtcNow();

            var reservation =
                new InventoryReservation(Guid.NewGuid(),
                    command.OrderId,
                    inventoryItem.Id,
                    command.Quantity,
                    createdAtUtc,
                    createdAtUtc.Add(
                        ReservationLifetime));

            await reservationRepository.AddAsync( reservation, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new ReserveInventoryResult(
                reservation.Id,
                reservation.OrderId,
                inventoryItem.Id,
                inventoryItem.SkuId,
                inventoryItem.WarehouseCode,
                reservation.Quantity,
                reservation.Status,
                reservation.ExpiresAtUtc,
                inventoryItem.OnHand,
                inventoryItem.Reserved,
                inventoryItem.Available);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);

            throw;
        }
    }
    public Task<ReservationTransitionResult?>
    ReleaseReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return TransitionReservationAsync(
            reservationId,
            ReservationTransition.Release,
            cancellationToken);
    }
    public Task<ReservationTransitionResult?>
    ConsumeReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return TransitionReservationAsync(
            reservationId,
            ReservationTransition.Consume,
            cancellationToken);
    }
    public Task<ReservationTransitionResult?>
    ExpireReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return TransitionReservationAsync(
            reservationId,
            ReservationTransition.Expire,
            cancellationToken);
    }
    private static ReservationTransitionResult
    CreateTransitionResult(
        InventoryReservation reservation,
        InventoryItem inventoryItem)
    {
        return new ReservationTransitionResult(
            reservation.Id,
            reservation.OrderId,
            reservation.InventoryItemId,
            reservation.Quantity,
            reservation.Status,
            inventoryItem.OnHand,
            inventoryItem.Reserved,
            inventoryItem.Available);
    }

    private async Task<ReservationTransitionResult?>
    TransitionReservationAsync(
        Guid reservationId,
        ReservationTransition transition,
        CancellationToken cancellationToken)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation id cannot be empty.",
                nameof(reservationId));
        }

        await using var transaction =
            await unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var reservation =
                await reservationRepository
                    .GetForUpdateAsync(
                        reservationId,
                        cancellationToken);

            if (reservation is null)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                return null;
            }

            var inventoryItem =
                await inventoryRepository
                    .GetByIdForUpdateAsync(
                        reservation.InventoryItemId,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    "Inventory item for reservation was not found.");

            var targetStatus =
                GetTargetStatus(
                    transition);

            if (reservation.Status == targetStatus)
            {
                await transaction.CommitAsync(
                    cancellationToken);

                return CreateTransitionResult(
                    reservation,
                    inventoryItem);
            }

            if (!reservation.IsActive)
            {
                throw new InvalidOperationException(
                    $"Reservation in status '{reservation.Status}' " +
                    $"cannot transition to '{targetStatus}'.");
            }

            ApplyTransition(
                reservation,
                inventoryItem,
                transition);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return CreateTransitionResult(
                reservation,
                inventoryItem);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }
    private static InventoryReservationStatus   GetTargetStatus( ReservationTransition transition)
    {
        return transition switch
        {
            ReservationTransition.Release =>
                InventoryReservationStatus.Released,

            ReservationTransition.Consume =>
                InventoryReservationStatus.Consumed,

            ReservationTransition.Expire =>
                InventoryReservationStatus.Expired,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(transition))
        };
    }
    private void ApplyTransition( InventoryReservation reservation, InventoryItem inventoryItem, ReservationTransition transition)
    {
        switch (transition)
        {
            case ReservationTransition.Release:
                reservation.Release();

                inventoryItem.Release(
                    reservation.Quantity);

                break;

            case ReservationTransition.Consume:
                reservation.Consume();

                inventoryItem.ConsumeReserved(
                    reservation.Quantity);

                break;

            case ReservationTransition.Expire:
                reservation.Expire(
                    timeProvider.GetUtcNow());

                inventoryItem.Release(
                    reservation.Quantity);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(transition));
        }
    }
    private enum ReservationTransition
    {
        Release,
        Consume,
        Expire
    }
}