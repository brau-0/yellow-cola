using YellowCola.Cart.Domain.Carts;

namespace YellowCola.Cart.Application.Carts;

public sealed class CartApplicationService(ICartRepository cartRepository)
{
    private const int MaxConcurrencyRetries = 5;

    public Task<ShoppingCart?> GetAsync(Guid cartId, CancellationToken cancellationToken = default)
        => cartRepository.GetAsync(cartId, cancellationToken);

    public async Task<ShoppingCart> CreateAsync(Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        if (!customerId.HasValue)
        {
            var anonymousCart = new ShoppingCart(Guid.NewGuid());

            if (!await cartRepository.CreateAsync(anonymousCart, cancellationToken))
                throw new InvalidOperationException("Anonymous cart could not be created.");

            return anonymousCart;
        }

        var existingCartId = await cartRepository.GetCustomerCartIdAsync(customerId.Value, cancellationToken);

        if (existingCartId.HasValue)
        {
            var existingCart = await cartRepository.GetAsync(existingCartId.Value, cancellationToken);

            if (existingCart is not null) return existingCart;
        }

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var cart = new ShoppingCart(Guid.NewGuid(), customerId);

            if (await cartRepository.TryCreateCustomerCartAsync(cart, cancellationToken)) return cart;

            existingCartId = await cartRepository.GetCustomerCartIdAsync(customerId.Value, cancellationToken);

            if (existingCartId.HasValue)
            {
                var existingCart = await cartRepository.GetAsync(existingCartId.Value, cancellationToken);

                if (existingCart is not null) return existingCart;
            }
        }

        throw new CartConflictException($"Active cart for customer '{customerId}' could not be created.");
    }

    public Task<ShoppingCart?> AddItemAsync(Guid cartId, Guid skuId, int quantity, CancellationToken cancellationToken = default)
        => UpdateAsync(cartId, cart => cart.AddItem(skuId, quantity), cancellationToken);

    public Task<ShoppingCart?> SetQuantityAsync(Guid cartId, Guid skuId, int quantity, CancellationToken cancellationToken = default)
        => UpdateAsync(cartId, cart => cart.SetQuantity(skuId, quantity), cancellationToken);

    public Task<ShoppingCart?> RemoveItemAsync(Guid cartId, Guid skuId, CancellationToken cancellationToken = default)
        => UpdateAsync(cartId, cart => cart.RemoveItem(skuId), cancellationToken);

    public async Task<ShoppingCart?> AssignCustomerAsync(Guid cartId, Guid customerId, CancellationToken cancellationToken = default)
    {
        if (cartId == Guid.Empty) throw new ArgumentException("Cart id cannot be empty.", nameof(cartId));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id cannot be empty.", nameof(customerId));

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var current = await cartRepository.GetVersionedAsync(cartId, cancellationToken);

            if (current is null) return null;

            if (current.Cart.CustomerId == customerId) return current.Cart;

            if (current.Cart.CustomerId.HasValue)
                throw new CartConflictException("Cart is already assigned to another customer.");

            var existingCustomerCartId = await cartRepository.GetCustomerCartIdAsync(customerId, cancellationToken);

            if (existingCustomerCartId.HasValue && existingCustomerCartId.Value != cartId)
                throw new CartConflictException("Customer already has another active cart.");

            current.Cart.AssignCustomer(customerId);

            if (await cartRepository.TryAssignCustomerAsync(current.Cart, current.Version, cancellationToken))
                return current.Cart;
        }

        throw new CartConflictException($"Cart '{cartId}' could not be assigned because of repeated concurrent modifications.");
    }

    private async Task<ShoppingCart?> UpdateAsync(Guid cartId, Action<ShoppingCart> mutation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var current = await cartRepository.GetVersionedAsync(cartId, cancellationToken);
            if (current is null) return null;

            try
            {
                mutation(current.Cart);
            }
            catch (InvalidOperationException exception)
            {
                throw new CartConflictException(exception.Message, exception);
            }

            if (await cartRepository.TrySaveAsync(current.Cart, current.Version, cancellationToken)) return current.Cart;
        }

        throw new CartConflictException($"Cart '{cartId}' could not be updated because of repeated concurrent modifications.");
    }

    public async Task<ShoppingCart?> MergeAnonymousCartAsync(Guid targetCartId, Guid sourceCartId, CancellationToken cancellationToken = default)
    {
        if (targetCartId == Guid.Empty) throw new ArgumentException("Target cart id cannot be empty.", nameof(targetCartId));
        if (sourceCartId == Guid.Empty) throw new ArgumentException("Source cart id cannot be empty.", nameof(sourceCartId));
        if (targetCartId == sourceCartId) throw new CartConflictException("A cart cannot be merged with itself.");

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var target = await cartRepository.GetVersionedAsync(targetCartId, cancellationToken);
            var source = await cartRepository.GetVersionedAsync(sourceCartId, cancellationToken);

            if (target is null || source is null) return null;

            try
            {
                target.Cart.MergeAnonymousCart(source.Cart);
            }
            catch (InvalidOperationException exception)
            {
                throw new CartConflictException(exception.Message, exception);
            }

            if (await cartRepository.TryMergeAsync(target.Cart, target.Version, sourceCartId, source.Version, cancellationToken))
                return target.Cart;
        }

        throw new CartConflictException("Carts could not be merged because of repeated concurrent modifications.");
    }
}