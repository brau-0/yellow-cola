using Microsoft.EntityFrameworkCore;

using YellowCola.Inventory.Application.Inventory;
using YellowCola.Inventory.Domain.InventoryItems;
using YellowCola.Inventory.Infrastructure.Persistence;

namespace YellowCola.Inventory.Infrastructure.Repositories;

internal sealed class InventoryRepository(
    InventoryDbContext dbContext)
    : IInventoryRepository
{
    public Task<InventoryItem?>
        GetForUpdateAsync(
            Guid skuId,
            string warehouseCode,
            CancellationToken cancellationToken = default)
    {
        return dbContext.InventoryItems
            .FromSqlInterpolated(
                $"""
                SELECT
                    id,
                    sku_id,
                    warehouse_code,
                    on_hand,
                    reserved
                FROM inventory_items
                WHERE sku_id = {skuId}
                  AND warehouse_code = {warehouseCode}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(
                cancellationToken);
    }
}