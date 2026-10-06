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

    public async Task<bool> CreateAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cart);
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

        var transaction = database.CreateTransaction();
        transaction.AddCondition(Condition.HashEqual(key, VersionField, expectedVersion));

        _ = transaction.HashSetAsync(key, DataField, json);
        _ = transaction.HashSetAsync(key, VersionField, nextVersion);
        _ = transaction.KeyExpireAsync(key, CartLifetime);

        return await transaction.ExecuteAsync();
    }

    public async Task<bool> DeleteAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        ValidateCartId(cartId);
        cancellationToken.ThrowIfCancellationRequested();

        return await connectionMultiplexer.GetDatabase().KeyDeleteAsync(GetKey(cartId));
    }

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