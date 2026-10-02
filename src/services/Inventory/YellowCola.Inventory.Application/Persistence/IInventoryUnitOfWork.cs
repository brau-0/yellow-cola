namespace YellowCola.Inventory.Application.Persistence;

public interface IInventoryUnitOfWork
{
    Task<IInventoryTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}