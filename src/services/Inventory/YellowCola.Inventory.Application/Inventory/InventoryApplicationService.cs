using YellowCola.Inventory.Application.Persistence;

namespace YellowCola.Inventory.Application.Inventory;

public sealed class InventoryApplicationService(
    IInventoryRepository repository,
    IInventoryUnitOfWork unitOfWork)
{
    public async Task<ReserveInventoryResult?>
        ReserveAsync(
            Guid skuId,
            string warehouseCode,
            int quantity,
            CancellationToken cancellationToken = default)
    {
        if (skuId == Guid.Empty)
        {
            throw new ArgumentException(
                "SKU id cannot be empty.",
                nameof(skuId));
        }

        if (string.IsNullOrWhiteSpace(warehouseCode))
        {
            throw new ArgumentException(
                "Warehouse code is required.",
                nameof(warehouseCode));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        await using var transaction =
            await unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var inventoryItem =
                await repository.GetForUpdateAsync(
                    skuId,
                    warehouseCode.Trim(),
                    cancellationToken);

            if (inventoryItem is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return null;
            }

            inventoryItem.Reserve(
                quantity);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new ReserveInventoryResult(
                inventoryItem.Id,
                inventoryItem.SkuId,
                inventoryItem.WarehouseCode,
                inventoryItem.OnHand,
                inventoryItem.Reserved,
                inventoryItem.Available);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }
}