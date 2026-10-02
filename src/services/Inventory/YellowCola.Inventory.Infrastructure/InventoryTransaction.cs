using Microsoft.EntityFrameworkCore.Storage;

using YellowCola.Inventory.Application.Persistence;

namespace YellowCola.Inventory.Infrastructure.Persistence;

internal sealed class InventoryTransaction(
    IDbContextTransaction transaction)
    : IInventoryTransaction
{
    public Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        return transaction.CommitAsync(
            cancellationToken);
    }

    public Task RollbackAsync(
        CancellationToken cancellationToken = default)
    {
        return transaction.RollbackAsync(
            cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return transaction.DisposeAsync();
    }
}