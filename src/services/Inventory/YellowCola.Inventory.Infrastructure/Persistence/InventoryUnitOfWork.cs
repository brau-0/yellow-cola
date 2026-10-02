using YellowCola.Inventory.Application.Persistence;

namespace YellowCola.Inventory.Infrastructure.Persistence;

internal sealed class InventoryUnitOfWork(
    InventoryDbContext dbContext)
    : IInventoryUnitOfWork
{
    public async Task<IInventoryTransaction>
        BeginTransactionAsync(
            CancellationToken cancellationToken = default)
    {
        var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        return new InventoryTransaction(
            transaction);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}