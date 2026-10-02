using YellowCola.Inventory.Domain.InventoryItems;

namespace YellowCola.Inventory.Application.Inventory;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetForUpdateAsync(
        Guid skuId,
        string warehouseCode,
        CancellationToken cancellationToken = default);
}