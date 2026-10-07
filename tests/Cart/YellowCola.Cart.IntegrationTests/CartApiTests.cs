using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

using Testcontainers.Redis;

using YellowCola.Cart.Api.Contracts;
using YellowCola.Cart.Application.Carts;
using YellowCola.Cart.Domain.Carts;
using YellowCola.Cart.IntegrationTests.Infrastructure;

namespace YellowCola.Cart.IntegrationTests;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "xUnit manages cleanup through IAsyncLifetime.")]
public sealed class CartApiTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8-alpine").Build();
    private CartWebApplicationFactory? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        _factory = new CartWebApplicationFactory(_redisContainer.GetConnectionString());
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task CreateCartShouldReturnCreated()
    {
        ArgumentNullException.ThrowIfNull(_client);

        var response = await _client.PostAsJsonAsync("/api/carts", new CreateCartRequest(null));
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(cart);
        Assert.NotEqual(Guid.Empty, cart.Id);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task GetMissingCartShouldReturnNotFound()
    {
        ArgumentNullException.ThrowIfNull(_client);

        var response = await _client.GetAsync($"/api/carts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddItemShouldReturnUpdatedCart()
    {
        ArgumentNullException.ThrowIfNull(_client);

        var created = await CreateCartAsync();
        var skuId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/carts/{created.Id}/items", new AddCartItemRequest(skuId, 3));
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(cart);
        Assert.Equal(3, Assert.Single(cart.Items).Quantity);
        Assert.Equal(3, cart.TotalQuantity);
    }

    [Fact]
    public async Task InvalidQuantityShouldReturnBadRequest()
    {
        ArgumentNullException.ThrowIfNull(_client);

        var created = await CreateCartAsync();

        var response = await _client.PostAsJsonAsync($"/api/carts/{created.Id}/items", new AddCartItemRequest(Guid.NewGuid(), 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignDifferentCustomerShouldReturnConflict()
    {
        ArgumentNullException.ThrowIfNull(_client);

        var created = await CreateCartAsync();

        var first = await _client.PutAsJsonAsync($"/api/carts/{created.Id}/customer", new AssignCartCustomerRequest(Guid.NewGuid()));
        var second = await _client.PutAsJsonAsync($"/api/carts/{created.Id}/customer", new AssignCartCustomerRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
    [Fact]
    public async Task ConcurrentHttpAddsShouldNotLoseUpdates()
    {
        ArgumentNullException.ThrowIfNull(_client);

        var created = await CreateCartAsync();
        var skuId = Guid.NewGuid();

        var initial = await _client.PostAsJsonAsync($"/api/carts/{created.Id}/items", new AddCartItemRequest(skuId, 2));
        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);

        var requestA = _client.PostAsJsonAsync($"/api/carts/{created.Id}/items", new AddCartItemRequest(skuId, 1));
        var requestB = _client.PostAsJsonAsync($"/api/carts/{created.Id}/items", new AddCartItemRequest(skuId, 2));

        var responses = await Task.WhenAll(requestA, requestB);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        var cart = await _client.GetFromJsonAsync<CartResponse>($"/api/carts/{created.Id}");

        Assert.NotNull(cart);
        Assert.Equal(5, Assert.Single(cart.Items).Quantity);
        Assert.Equal(5, cart.TotalQuantity);
    }
    [Fact]
    public async Task MergeAnonymousCartShouldCombineItemsAndDeleteSource()
    {
        ArgumentNullException.ThrowIfNull(_client);

        var customerId = Guid.NewGuid();
        var skuA = Guid.NewGuid();
        var skuB = Guid.NewGuid();
        var skuC = Guid.NewGuid();

        var target = await CreateCartAsync(customerId);
        var source = await CreateCartAsync();

        await _client.PostAsJsonAsync($"/api/carts/{target.Id}/items", new AddCartItemRequest(skuA, 3));
        await _client.PostAsJsonAsync($"/api/carts/{target.Id}/items", new AddCartItemRequest(skuC, 2));

        await _client.PostAsJsonAsync($"/api/carts/{source.Id}/items", new AddCartItemRequest(skuA, 2));
        await _client.PostAsJsonAsync($"/api/carts/{source.Id}/items", new AddCartItemRequest(skuB, 1));

        var response = await _client.PostAsJsonAsync($"/api/carts/{target.Id}/merge", new MergeCartRequest(source.Id));
        var merged = await response.Content.ReadFromJsonAsync<CartResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(merged);
        Assert.Equal(5, merged.Items.Single(item => item.SkuId == skuA).Quantity);
        Assert.Equal(1, merged.Items.Single(item => item.SkuId == skuB).Quantity);
        Assert.Equal(2, merged.Items.Single(item => item.SkuId == skuC).Quantity);
        Assert.Equal(8, merged.TotalQuantity);

        var sourceResponse = await _client.GetAsync($"/api/carts/{source.Id}");

        Assert.Equal(HttpStatusCode.NotFound, sourceResponse.StatusCode);
    }
    [Fact]
    public async Task MergeCustomerCartIntoAnotherCustomerCartShouldReturnConflict()
    {
        ArgumentNullException.ThrowIfNull(_client);

        var target = await CreateCartAsync(Guid.NewGuid());
        var source = await CreateCartAsync(Guid.NewGuid());

        var response = await _client.PostAsJsonAsync($"/api/carts/{target.Id}/merge", new MergeCartRequest(source.Id));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
    [Fact]
    public async Task ConcurrentCustomerCartCreationShouldReturnSingleActiveCart()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var customerId = Guid.NewGuid();
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var attemptA = CreateCustomerCartAfterSignalAsync(customerId, startSignal.Task);
        var attemptB = CreateCustomerCartAfterSignalAsync(customerId, startSignal.Task);

        startSignal.SetResult();

        var carts = await Task.WhenAll(attemptA, attemptB);

        Assert.Equal(carts[0].Id, carts[1].Id);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICartRepository>();
        var activeCartId = await repository.GetCustomerCartIdAsync(customerId);

        Assert.Equal(carts[0].Id, activeCartId);
    }
    [Fact]
    public async Task CreatingCartTwiceForSameCustomerShouldReturnSameCart()
    {
        ArgumentNullException.ThrowIfNull(_client);

        var customerId = Guid.NewGuid();

        var firstResponse = await _client.PostAsJsonAsync("/api/carts", new CreateCartRequest(customerId));
        var secondResponse = await _client.PostAsJsonAsync("/api/carts", new CreateCartRequest(customerId));

        var first = await firstResponse.Content.ReadFromJsonAsync<CartResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Id, second.Id);
    }
    private async Task<ShoppingCart> CreateCustomerCartAfterSignalAsync(Guid customerId, Task startSignal)
    {
        ArgumentNullException.ThrowIfNull(_factory);

        await startSignal;

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CartApplicationService>();

        return await service.CreateAsync(customerId);
    }
    private async Task<CartResponse> CreateCartAsync(Guid? customerId = null)
    {
        ArgumentNullException.ThrowIfNull(_client);

        var response = await _client.PostAsJsonAsync("/api/carts", new CreateCartRequest(customerId));
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();

        return cart ?? throw new InvalidOperationException("Cart was not returned.");
    }
}