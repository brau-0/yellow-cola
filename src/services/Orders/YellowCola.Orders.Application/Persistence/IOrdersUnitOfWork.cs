namespace YellowCola.Orders.Application.Persistence;

public interface IOrdersUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}