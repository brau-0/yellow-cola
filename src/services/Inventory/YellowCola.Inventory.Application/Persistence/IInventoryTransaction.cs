namespace YellowCola.Inventory.Application.Persistence;

public interface IInventoryTransaction
    : IAsyncDisposable
{
    Task CommitAsync(
        CancellationToken cancellationToken = default);

    Task RollbackAsync(
        CancellationToken cancellationToken = default);
}