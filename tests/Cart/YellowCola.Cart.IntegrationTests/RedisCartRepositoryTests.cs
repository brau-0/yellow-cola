using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using Testcontainers.Redis;

using YellowCola.Cart.Application.Carts;
using YellowCola.Cart.Domain.Carts;
using YellowCola.Cart.IntegrationTests.Infrastructure;

namespace YellowCola.Cart.IntegrationTests;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "xUnit manages the test lifetime through IAsyncLifetime, and disposable resources are released in DisposeAsync.")]
public sealed class RedisCartRepositoryTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8-alpine").Build();
    private CartWebApplicationFactory? _factory;

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        _factory = new CartWebApplicationFactory(_redisContainer.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task SaveAndGetShouldRoundTripCart()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var cartId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var skuA = Guid.NewGuid();
        var skuB = Guid.NewGuid();

        var cart = new ShoppingCart(cartId, customerId);
        cart.AddItem(skuA, 2);
        cart.AddItem(skuB, 3);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();

        var created = await repository.TryCreateCustomerCartAsync(cart);
        Assert.True(created);
        var restored = await repository.GetAsync(cartId);

        Assert.NotNull(restored);
        Assert.Equal(cartId, restored.Id);
        Assert.Equal(customerId, restored.CustomerId);
        Assert.Equal(2, restored.Items.Count);
        Assert.Equal(5, restored.TotalQuantity);
        Assert.Equal(2, restored.Items.Single(item => item.SkuId == skuA).Quantity);
        Assert.Equal(3, restored.Items.Single(item => item.SkuId == skuB).Quantity);
    }

    [Fact]
    public async Task GetMissingCartShouldReturnNull()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();

        var cart = await repository.GetAsync(Guid.NewGuid());

        Assert.Null(cart);
    }

    [Fact]
    public async Task DeleteShouldRemoveCart()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var cart = new ShoppingCart(Guid.NewGuid());
        cart.AddItem(Guid.NewGuid(), 2);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();

        var created = await repository.CreateAnonymousAsync(cart);
        Assert.True(created);

        var deleted = await repository.DeleteAnonymousAsync(cart.Id);
        var restored = await repository.GetAsync(cart.Id);

        Assert.True(deleted);
        Assert.Null(restored);
    }
    [Fact]
    public async Task SaveShouldSetExpiration()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var cart = new ShoppingCart(Guid.NewGuid(), Guid.NewGuid());
        cart.AddItem(Guid.NewGuid(), 1);

        using var scope = _factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();
        var connection = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var created = await repository.TryCreateCustomerCartAsync(cart);
        Assert.True(created);

        var database = connection.GetDatabase();
        var key = $"yc:cart:{cart.Id:N}";
        var ttl = await database.KeyTimeToLiveAsync(key);

        Assert.NotNull(ttl);
        Assert.True(ttl > TimeSpan.FromDays(29));
        Assert.True(ttl <= TimeSpan.FromDays(30));
    }
    [Fact]
    public async Task ConcurrentAddsShouldNotLoseUpdates()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        Guid cartId;
        var skuId = Guid.NewGuid();

        using (var createScope = _factory.Services.CreateScope())
        {
            var service = createScope.ServiceProvider.GetRequiredService<CartApplicationService>();
            var cart = await service.CreateAsync();
            cartId = cart.Id;

            await service.AddItemAsync(cartId, skuId, 2);
        }

        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var attemptA = AddAfterSignalAsync(cartId, skuId, 1, startSignal.Task);
        var attemptB = AddAfterSignalAsync(cartId, skuId, 2, startSignal.Task);

        startSignal.SetResult();

        await Task.WhenAll(attemptA, attemptB);

        using var verificationScope = _factory.Services.CreateScope();
        var repository = verificationScope.ServiceProvider.GetRequiredService<ICartRepository>();
        var restored = await repository.GetAsync(cartId);

        Assert.NotNull(restored);
        Assert.Equal(5, Assert.Single(restored.Items).Quantity);
        Assert.Equal(5, restored.TotalQuantity);
    }

    [Fact]
    public async Task SuccessfulUpdateShouldIncrementVersion()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var cart = new ShoppingCart(Guid.NewGuid(), Guid.NewGuid());
        cart.AddItem(Guid.NewGuid(), 1);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();

        Assert.True(await repository.TryCreateCustomerCartAsync(cart));

        var before = await repository.GetVersionedAsync(cart.Id);

        Assert.NotNull(before);
        Assert.Equal(1, before.Version);

        before.Cart.AddItem(Guid.NewGuid(), 2);

        Assert.True(await repository.TrySaveAsync(before.Cart, before.Version));

        var after = await repository.GetVersionedAsync(cart.Id);

        Assert.NotNull(after);
        Assert.Equal(2, after.Version);
    }
    [Fact]
    public async Task CustomerIndexTtlShouldRefreshWhenCustomerCartIsUpdated()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var customerId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CartApplicationService>();
        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();
        var connection = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var cart = await service.CreateAsync(customerId);

        var database = connection.GetDatabase();
        var customerKey = $"yc:customer-cart:{customerId:N}";

        Assert.True(await database.KeyExpireAsync(customerKey, TimeSpan.FromMinutes(1)));

        await service.AddItemAsync(cart.Id, skuId, 1);

        var ttl = await database.KeyTimeToLiveAsync(customerKey);

        Assert.NotNull(ttl);
        Assert.True(ttl > TimeSpan.FromDays(29));
        Assert.True(ttl <= TimeSpan.FromDays(30));

        Assert.Equal(cart.Id, await repository.GetCustomerCartIdAsync(customerId));
    }
    [Fact]
    public async Task StaleCustomerIndexShouldBeRepairedWhenCreatingCart()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var customerId = Guid.NewGuid();
        var staleCartId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CartApplicationService>();
        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();
        var connection = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var database = connection.GetDatabase();
        var customerKey = $"yc:customer-cart:{customerId:N}";

        await database.StringSetAsync(customerKey, staleCartId.ToString("D"), TimeSpan.FromDays(30));

        var cart = await service.CreateAsync(customerId);
        var activeCartId = await repository.GetCustomerCartIdAsync(customerId);

        Assert.NotEqual(staleCartId, cart.Id);
        Assert.Equal(cart.Id, activeCartId);
        Assert.Equal(customerId, cart.CustomerId);
    }
    [Fact]
    public async Task MergeShouldRefreshCustomerIndexExpiration()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var customerId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CartApplicationService>();
        var connection = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var target = await service.CreateAsync(customerId);
        var source = await service.CreateAsync();

        await service.AddItemAsync(source.Id, Guid.NewGuid(), 2);

        var database = connection.GetDatabase();
        var customerKey = $"yc:customer-cart:{customerId:N}";

        Assert.True(await database.KeyExpireAsync(customerKey, TimeSpan.FromMinutes(1)));

        var merged = await service.MergeAnonymousCartAsync(target.Id, source.Id);

        Assert.NotNull(merged);

        var ttl = await database.KeyTimeToLiveAsync(customerKey);

        Assert.NotNull(ttl);
        Assert.True(ttl > TimeSpan.FromDays(29));
        Assert.True(ttl <= TimeSpan.FromDays(30));
    }
    [Fact]
    public async Task CreateAnonymousShouldRejectCustomerCart()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var cart = new ShoppingCart(Guid.NewGuid(), Guid.NewGuid());

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();

        await Assert.ThrowsAsync<ArgumentException>(() => repository.CreateAnonymousAsync(cart));
    }
    [Fact]
    public async Task CreateCustomerCartShouldRejectAnonymousCart()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var cart = new ShoppingCart(Guid.NewGuid());

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();

        await Assert.ThrowsAsync<ArgumentException>(() => repository.TryCreateCustomerCartAsync(cart));
    }
    private async Task<ShoppingCart?> AddAfterSignalAsync(Guid cartId, Guid skuId, int quantity, Task startSignal)
    {
        ArgumentNullException.ThrowIfNull(_factory);

        await startSignal;

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CartApplicationService>();

        return await service.AddItemAsync(cartId, skuId, quantity);
    }
}