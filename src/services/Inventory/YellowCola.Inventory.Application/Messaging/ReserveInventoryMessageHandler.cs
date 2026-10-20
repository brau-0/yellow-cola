using YellowCola.Checkout.Contracts.Inventory;
using YellowCola.Inventory.Application.Inventory;
using YellowCola.Inventory.Application.Persistence;
using YellowCola.Inventory.Application.Reservations;
using YellowCola.Inventory.Domain.InventoryItems;
using YellowCola.Inventory.Domain.Reservations;

using IntegrationReserveInventoryCommand = YellowCola.Checkout.Contracts.Inventory.ReserveInventoryCommand;

namespace YellowCola.Inventory.Application.Messaging;

public enum InventoryMessageHandlingOutcome
{
    Processed,
    Duplicate
}

public sealed class ReserveInventoryMessageHandler(
    IInventoryRepository inventoryRepository,
    IInventoryReservationRepository reservationRepository,
    IInventoryUnitOfWork unitOfWork,
    IInboxStore inboxStore,
    IInventoryOutboxWriter outboxWriter,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan ReservationLifetime = TimeSpan.FromMinutes(15);

    public async Task<InventoryMessageHandlingOutcome> HandleAsync(IntegrationReserveInventoryCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var receivedAtUtc = timeProvider.GetUtcNow();

            var started = await inboxStore.TryStartAsync(command.MessageId, command.MessageType, command.CorrelationId, receivedAtUtc, cancellationToken);

            if (!started)
            {
                await transaction.CommitAsync(cancellationToken);
                return InventoryMessageHandlingOutcome.Duplicate;
            }

            var warehouseCode = command.WarehouseCode.Trim();
            var orderedLines = command.Lines.OrderBy(line => line.SkuId).ToArray();
            var lockedItems = new List<(ReserveInventoryLine Line, InventoryItem Item)>(orderedLines.Length);

            foreach (var line in orderedLines)
            {
                var item = await inventoryRepository.GetForUpdateAsync(line.SkuId, warehouseCode, cancellationToken);

                if (item is null)
                    return await CompleteFailureAsync(command, "SKU_NOT_FOUND", $"SKU '{line.SkuId}' was not found in warehouse '{warehouseCode}'.", transaction, cancellationToken);

                lockedItems.Add((line, item));
            }

            foreach (var entry in lockedItems)
            {
                if (entry.Line.Quantity > entry.Item.Available)
                    return await CompleteFailureAsync(command, "INSUFFICIENT_STOCK", $"Insufficient stock for SKU '{entry.Line.SkuId}'.", transaction, cancellationToken);
            }

            var createdAtUtc = timeProvider.GetUtcNow();
            var expiresAtUtc = createdAtUtc.Add(ReservationLifetime);
            var reservationResults = new List<InventoryReservationResult>(lockedItems.Count);

            foreach (var entry in lockedItems)
            {
                entry.Item.Reserve(entry.Line.Quantity);

                var reservation = new InventoryReservation(Guid.NewGuid(), command.OrderId, entry.Item.Id, entry.Line.Quantity, createdAtUtc, expiresAtUtc);

                await reservationRepository.AddAsync(reservation, cancellationToken);

                reservationResults.Add(new InventoryReservationResult(reservation.Id, entry.Item.SkuId, reservation.Quantity, reservation.ExpiresAtUtc));
            }

            var reservedEvent = new InventoryReservedEvent(Guid.NewGuid(), command.CorrelationId, command.MessageId, createdAtUtc, command.OrderId, reservationResults);

            outboxWriter.Add(reservedEvent);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await inboxStore.MarkProcessedAsync(command.MessageId, timeProvider.GetUtcNow(), cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return InventoryMessageHandlingOutcome.Processed;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<InventoryMessageHandlingOutcome> CompleteFailureAsync(IntegrationReserveInventoryCommand command, string failureCode, string reason, IInventoryTransaction transaction, CancellationToken cancellationToken)
    {
        var occurredAtUtc = timeProvider.GetUtcNow();

        var failedEvent = new InventoryReservationFailedEvent(Guid.NewGuid(), command.CorrelationId, command.MessageId, occurredAtUtc, command.OrderId, failureCode, reason);

        outboxWriter.Add(failedEvent);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await inboxStore.MarkProcessedAsync(command.MessageId, occurredAtUtc, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return InventoryMessageHandlingOutcome.Processed;
    }

    private static void Validate(IntegrationReserveInventoryCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.MessageId == Guid.Empty) throw new ArgumentException("Message id cannot be empty.", nameof(command));
        if (command.CorrelationId == Guid.Empty) throw new ArgumentException("Correlation id cannot be empty.", nameof(command));
        if (command.OrderId == Guid.Empty) throw new ArgumentException("Order id cannot be empty.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.WarehouseCode)) throw new ArgumentException("Warehouse code is required.", nameof(command));
        if (command.Lines.Count == 0) throw new ArgumentException("At least one inventory line is required.", nameof(command));
        if (command.Lines.Any(line => line.SkuId == Guid.Empty || line.Quantity <= 0)) throw new ArgumentException("All inventory lines must contain a valid SKU id and positive quantity.", nameof(command));
        if (command.Lines.GroupBy(line => line.SkuId).Any(group => group.Count() > 1)) throw new ArgumentException("Inventory command cannot contain duplicate SKU lines.", nameof(command));
    }
}