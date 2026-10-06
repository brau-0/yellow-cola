using YellowCola.Cart.Domain.Carts;

namespace YellowCola.Cart.Application.Carts;

public sealed class CartApplicationService(ICartRepository cartRepository)
{
    private const int MaxConcurrencyRetries = 5;

    public Task<ShoppingCart?> GetAsync(Guid cartId, CancellationToken cancellationToken = default)
        => cartRepository.GetAsync(cartId, cancellationToken);

    public async Task<ShoppingCart> CreateAsync(Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var cart = new ShoppingCart(Guid.NewGuid(), customerId);

        if (!await cartRepository.CreateAsync(cart, cancellationToken))
            throw new InvalidOperationException("Cart could not be created.");

        return cart;
    }

    public Task<ShoppingCart?> AddItemAsync(Guid cartId, Guid skuId, int quantity, CancellationToken cancellationToken = default)
        => UpdateAsync(cartId, cart => cart.AddItem(skuId, quantity), cancellationToken);

    public Task<ShoppingCart?> SetQuantityAsync(Guid cartId, Guid skuId, int quantity, CancellationToken cancellationToken = default)
        => UpdateAsync(cartId, cart => cart.SetQuantity(skuId, quantity), cancellationToken);

    public Task<ShoppingCart?> RemoveItemAsync(Guid cartId, Guid skuId, CancellationToken cancellationToken = default)
        => UpdateAsync(cartId, cart => cart.RemoveItem(skuId), cancellationToken);

    public Task<ShoppingCart?> AssignCustomerAsync(Guid cartId, Guid customerId, CancellationToken cancellationToken = default)
        => UpdateAsync(cartId, cart => cart.AssignCustomer(customerId), cancellationToken);

    private async Task<ShoppingCart?> UpdateAsync(Guid cartId, Action<ShoppingCart> mutation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var current = await cartRepository.GetVersionedAsync(cartId, cancellationToken);
            if (current is null) return null;

            mutation(current.Cart);

            if (await cartRepository.TrySaveAsync(current.Cart, current.Version, cancellationToken))
                return current.Cart;
        }

        throw new InvalidOperationException($"Cart '{cartId}' could not be updated because of repeated concurrent modifications.");
    }
}