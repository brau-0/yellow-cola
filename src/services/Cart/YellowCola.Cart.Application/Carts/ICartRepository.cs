using YellowCola.Cart.Domain.Carts;

namespace YellowCola.Cart.Application.Carts;

public interface ICartRepository
{
    Task<ShoppingCart?> GetAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<VersionedCart?> GetVersionedAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
    Task<bool> TrySaveAsync(ShoppingCart cart, long expectedVersion, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid cartId, CancellationToken cancellationToken = default);
}