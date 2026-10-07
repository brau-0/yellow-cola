using System.Globalization;
using System.Text.Json;

using StackExchange.Redis;

using YellowCola.Cart.Application.Carts;
using YellowCola.Cart.Domain.Carts;

namespace YellowCola.Cart.Infrastructure.Persistence;

internal sealed class RedisCartRepository(IConnectionMultiplexer connectionMultiplexer) : ICartRepository
{
    private static readonly TimeSpan CartLifetime = TimeSpan.FromDays(30);
    private static readonly RedisValue[] ReadFields = [DataField, VersionField];
    private const string CustomerCartKeyPrefix = "yc:customer-cart:";

    private static RedisKey GetCustomerCartKey(Guid customerId) => $"{CustomerCartKeyPrefix}{customerId:N}";

    private const string KeyPrefix = "yc:cart:";
    private const string DataField = "data";
    private const string VersionField = "version";

    public async Task<ShoppingCart?> GetAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var versionedCart = await GetVersionedAsync(cartId, cancellationToken);
        return versionedCart?.Cart;
    }

    public async Task<VersionedCart?> GetVersionedAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        ValidateCartId(cartId);
        cancellationToken.ThrowIfCancellationRequested();

        var database = connectionMultiplexer.GetDatabase();
        var values = await database.HashGetAsync(GetKey(cartId), ReadFields);

        if (values[0].IsNullOrEmpty) return null;
        if (values[1].IsNullOrEmpty) throw new InvalidOperationException($"Cart '{cartId}' has no concurrency version.");

        var document = JsonSerializer.Deserialize<RedisCartDocument>(values[0].ToString())
            ?? throw new InvalidOperationException($"Cart '{cartId}' could not be deserialized.");

        if (!long.TryParse(values[1].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var version))
            throw new InvalidOperationException($"Cart '{cartId}' has an invalid concurrency version.");

        return new VersionedCart(ToDomain(document), version);
    }

    public async Task<bool> CreateAnonymousAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cart);

        if (cart.CustomerId.HasValue) throw new ArgumentException("Anonymous cart cannot have a customer id.", nameof(cart));

        cancellationToken.ThrowIfCancellationRequested();

        var database = connectionMultiplexer.GetDatabase();
        var key = GetKey(cart.Id);
        var json = JsonSerializer.Serialize(ToDocument(cart));

        var transaction = database.CreateTransaction();
        transaction.AddCondition(Condition.KeyNotExists(key));

        _ = transaction.HashSetAsync(key, DataField, json);
        _ = transaction.HashSetAsync(key, VersionField, 1L);
        _ = transaction.KeyExpireAsync(key, CartLifetime);

        return await transaction.ExecuteAsync();
    }

    public async Task<bool> TrySaveAsync(ShoppingCart cart, long expectedVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);

        cancellationToken.ThrowIfCancellationRequested();

        var database = connectionMultiplexer.GetDatabase();
        var key = GetKey(cart.Id);
        var json = JsonSerializer.Serialize(ToDocument(cart));
        var nextVersion = checked(expectedVersion + 1);

        RedisKey? customerKey = null;
        RedisValue currentCustomerCartId = RedisValue.Null;
        var cartIdValue = cart.Id.ToString("D");

        if (cart.CustomerId.HasValue)
        {
            customerKey = GetCustomerCartKey(cart.CustomerId.Value);
            currentCustomerCartId = await database.StringGetAsync(customerKey.Value);

            if (!currentCustomerCartId.IsNullOrEmpty && currentCustomerCartId != cartIdValue) return false;
        }

        var transaction = database.CreateTransaction();
        transaction.AddCondition(Condition.HashEqual(key, VersionField, expectedVersion));

        if (customerKey.HasValue)
        {
            if (currentCustomerCartId.IsNullOrEmpty)
            {
                transaction.AddCondition(Condition.KeyNotExists(customerKey.Value));
                _ = transaction.StringSetAsync(customerKey.Value, cartIdValue, CartLifetime);
            }
            else
            {
                transaction.AddCondition(Condition.StringEqual(customerKey.Value, cartIdValue));
                _ = transaction.KeyExpireAsync(customerKey.Value, CartLifetime);
            }
        }

        _ = transaction.HashSetAsync(key, DataField, json);
        _ = transaction.HashSetAsync(key, VersionField, nextVersion);
        _ = transaction.KeyExpireAsync(key, CartLifetime);

        return await transaction.ExecuteAsync();
    }
    public async Task<bool> TryMergeAsync(ShoppingCart targetCart, long expectedTargetVersion, Guid sourceCartId, long expectedSourceVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetCart);

        if (!targetCart.CustomerId.HasValue) throw new ArgumentException("Target cart must have a customer id.", nameof(targetCart));
        if (sourceCartId == Guid.Empty) throw new ArgumentException("Source cart id cannot be empty.", nameof(sourceCartId));
        if (targetCart.Id == sourceCartId) throw new ArgumentException("Target and source cart must be different.");

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedTargetVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedSourceVersion);

        cancellationToken.ThrowIfCancellationRequested();

        var database = connectionMultiplexer.GetDatabase();
        var targetKey = GetKey(targetCart.Id);
        var sourceKey = GetKey(sourceCartId);
        var customerKey = GetCustomerCartKey(targetCart.CustomerId.Value);
        var targetCartIdValue = targetCart.Id.ToString("D");
        var existingCustomerCartId = await database.StringGetAsync(customerKey);

        if (!existingCustomerCartId.IsNullOrEmpty && existingCustomerCartId != targetCartIdValue) return false;

        var json = JsonSerializer.Serialize(ToDocument(targetCart));
        var nextVersion = checked(expectedTargetVersion + 1);

        var transaction = database.CreateTransaction();

        transaction.AddCondition(Condition.HashEqual(targetKey, VersionField, expectedTargetVersion));
        transaction.AddCondition(Condition.HashEqual(sourceKey, VersionField, expectedSourceVersion));

        if (existingCustomerCartId.IsNullOrEmpty)
        {
            transaction.AddCondition(Condition.KeyNotExists(customerKey));
            _ = transaction.StringSetAsync(customerKey, targetCartIdValue, CartLifetime);
        }
        else
        {
            transaction.AddCondition(Condition.StringEqual(customerKey, targetCartIdValue));
            _ = transaction.KeyExpireAsync(customerKey, CartLifetime);
        }

        _ = transaction.HashSetAsync(targetKey, DataField, json);
        _ = transaction.HashSetAsync(targetKey, VersionField, nextVersion);
        _ = transaction.KeyExpireAsync(targetKey, CartLifetime);
        _ = transaction.KeyDeleteAsync(sourceKey);

        return await transaction.ExecuteAsync();
    }
    public async Task<bool> DeleteAnonymousAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        ValidateCartId(cartId);
        cancellationToken.ThrowIfCancellationRequested();

        var current = await GetVersionedAsync(cartId, cancellationToken);

        if (current is null) return false;
        if (current.Cart.CustomerId.HasValue) throw new InvalidOperationException("Customer cart cannot be deleted as an anonymous cart.");

        var database = connectionMultiplexer.GetDatabase();
        var key = GetKey(cartId);

        var transaction = database.CreateTransaction();
        transaction.AddCondition(Condition.HashEqual(key, VersionField, current.Version));
        _ = transaction.KeyDeleteAsync(key);

        return await transaction.ExecuteAsync();
    }
    public async Task<Guid?> GetCustomerCartIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id cannot be empty.", nameof(customerId));

        cancellationToken.ThrowIfCancellationRequested();

        var value = await connectionMultiplexer.GetDatabase().StringGetAsync(GetCustomerCartKey(customerId));

        if (value.IsNullOrEmpty) return null;

        if (!Guid.TryParse(value.ToString(), out var cartId))
            throw new InvalidOperationException($"Customer '{customerId}' has an invalid cart reference.");

        return cartId;
    }

    public async Task<bool> TryCreateCustomerCartAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cart);

        if (!cart.CustomerId.HasValue) throw new ArgumentException("Customer cart must have a customer id.", nameof(cart));

        cancellationToken.ThrowIfCancellationRequested();

        var database = connectionMultiplexer.GetDatabase();
        var cartKey = GetKey(cart.Id);
        var customerKey = GetCustomerCartKey(cart.CustomerId.Value);
        var json = JsonSerializer.Serialize(ToDocument(cart));

        var transaction = database.CreateTransaction();

        transaction.AddCondition(Condition.KeyNotExists(cartKey));
        transaction.AddCondition(Condition.KeyNotExists(customerKey));

        _ = transaction.HashSetAsync(cartKey, DataField, json);
        _ = transaction.HashSetAsync(cartKey, VersionField, 1L);
        _ = transaction.KeyExpireAsync(cartKey, CartLifetime);
        _ = transaction.StringSetAsync(customerKey, cart.Id.ToString("D"), CartLifetime);

        return await transaction.ExecuteAsync();
    }
    public async Task<bool> TryAssignCustomerAsync(ShoppingCart cart, long expectedVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cart);

        if (!cart.CustomerId.HasValue) throw new ArgumentException("Cart must have a customer id.", nameof(cart));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        cancellationToken.ThrowIfCancellationRequested();

        var database = connectionMultiplexer.GetDatabase();
        var cartKey = GetKey(cart.Id);
        var customerKey = GetCustomerCartKey(cart.CustomerId.Value);
        var cartIdValue = cart.Id.ToString("D");
        var existingCustomerCartId = await database.StringGetAsync(customerKey);

        if (!existingCustomerCartId.IsNullOrEmpty && existingCustomerCartId != cartIdValue) return false;

        var json = JsonSerializer.Serialize(ToDocument(cart));
        var nextVersion = checked(expectedVersion + 1);

        var transaction = database.CreateTransaction();
        transaction.AddCondition(Condition.HashEqual(cartKey, VersionField, expectedVersion));

        if (existingCustomerCartId.IsNullOrEmpty)
        {
            transaction.AddCondition(Condition.KeyNotExists(customerKey));
            _ = transaction.StringSetAsync(customerKey, cartIdValue, CartLifetime);
        }
        else
        {
            transaction.AddCondition(Condition.StringEqual(customerKey, cartIdValue));
            _ = transaction.KeyExpireAsync(customerKey, CartLifetime);
        }

        _ = transaction.HashSetAsync(cartKey, DataField, json);
        _ = transaction.HashSetAsync(cartKey, VersionField, nextVersion);
        _ = transaction.KeyExpireAsync(cartKey, CartLifetime);

        return await transaction.ExecuteAsync();
    }
#pragma warning disable SER301 // Keep transaction-based compare-and-delete for compatibility with Redis versions before 8.4.
    public async Task<bool> TryRemoveCustomerCartIndexAsync(Guid customerId, Guid expectedCartId, CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id cannot be empty.", nameof(customerId));
        if (expectedCartId == Guid.Empty) throw new ArgumentException("Expected cart id cannot be empty.", nameof(expectedCartId));

        cancellationToken.ThrowIfCancellationRequested();

        var database = connectionMultiplexer.GetDatabase();
        var customerKey = GetCustomerCartKey(customerId);

        var transaction = database.CreateTransaction();
        transaction.AddCondition(Condition.StringEqual(customerKey, expectedCartId.ToString("D")));
        _ = transaction.KeyDeleteAsync(customerKey);

        return await transaction.ExecuteAsync();
    }
#pragma warning restore SER301
    private static RedisKey GetKey(Guid cartId) => $"{KeyPrefix}{cartId:N}";

    private static void ValidateCartId(Guid cartId)
    {
        if (cartId == Guid.Empty) throw new ArgumentException("Cart id cannot be empty.", nameof(cartId));
    }

    private static RedisCartDocument ToDocument(ShoppingCart cart)
    {
        var items = cart.Items.Select(item => new RedisCartItemDocument(item.SkuId, item.Quantity)).ToList();
        return new RedisCartDocument(cart.Id, cart.CustomerId, items);
    }

    private static ShoppingCart ToDomain(RedisCartDocument document)
    {
        var cart = new ShoppingCart(document.Id, document.CustomerId);

        foreach (var item in document.Items) cart.AddItem(item.SkuId, item.Quantity);

        return cart;
    }
}