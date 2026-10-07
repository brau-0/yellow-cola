using YellowCola.Cart.Domain.Carts;

namespace YellowCola.Cart.Application.Carts;

public interface ICartRepository
{
    Task<ShoppingCart?> GetAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<VersionedCart?> GetVersionedAsync(Guid cartId, CancellationToken cancellationToken = default);

    Task<bool> CreateAnonymousAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
    Task<bool> TryCreateCustomerCartAsync(ShoppingCart cart, CancellationToken cancellationToken = default);

    Task<bool> TrySaveAsync(ShoppingCart cart, long expectedVersion, CancellationToken cancellationToken = default);
    Task<bool> TryAssignCustomerAsync(ShoppingCart cart, long expectedVersion, CancellationToken cancellationToken = default);
    Task<bool> TryMergeAsync(ShoppingCart targetCart, long expectedTargetVersion, Guid sourceCartId, long expectedSourceVersion, CancellationToken cancellationToken = default);

    Task<Guid?> GetCustomerCartIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> TryRemoveCustomerCartIndexAsync(Guid customerId, Guid expectedCartId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAnonymousAsync(Guid cartId, CancellationToken cancellationToken = default);
}