using YellowCola.Orders.Application.Checkout;
using YellowCola.Orders.Application.Orders;
using YellowCola.Orders.Application.Persistence;
using YellowCola.Orders.Domain.Orders;

namespace YellowCola.Orders.UnitTests;

public sealed class CheckoutApplicationServiceTests
{
    [Fact]
    public async Task CheckoutShouldCreateHistoricalOrderSnapshot()
    {
        var customerId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        var cartReader = new FakeCartReader(new CheckoutCart(cartId, customerId, [new CheckoutCartItem(skuId, 2)]));
        var catalogReader = new FakeCatalogReader([new CheckoutCatalogItem(skuId, "YC-001", "Yellow Cola Original 6 Pack", 129.90m, "MXN", true)]);
        var customerReader = new FakeCustomerReader(new CheckoutAddress(addressId, customerId, "Braulio Guzman", "52900", "Estado de México", "Atizapán de Zaragoza", "Centro", "Av. Principal", "100", null, "Casa blanca"));
        var repository = new FakeOrderRepository();
        var unitOfWork = new FakeUnitOfWork();

        var service = new CheckoutApplicationService(cartReader, catalogReader, customerReader, repository, unitOfWork);

        var result = await service.CheckoutAsync(new CheckoutRequest(cartId, customerId, addressId));

        Assert.Equal(customerId, result.Order.CustomerId);
        Assert.Equal(OrderStatus.Pending, result.Order.Status);
        Assert.Equal(259.80m, result.Order.TotalAmount);
        Assert.Equal(129.90m, result.Order.Lines.Single().UnitPrice);
        Assert.Equal("52900", result.Order.ShippingAddress.PostalCode);
        Assert.Same(result.Order, repository.AddedOrder);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
    [Fact]
    public async Task CheckoutShouldFailWhenCartDoesNotExist()
    {
        var service = CreateService(cart: null);

        await Assert.ThrowsAsync<CheckoutException>(() => service.CheckoutAsync(new CheckoutRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
    }
    [Fact]
    public async Task CheckoutShouldRejectAnonymousCart()
    {
        var cartId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var cartReader = new FakeCartReader(new CheckoutCart(cartId, null, [new CheckoutCartItem(Guid.NewGuid(), 1)]));
        var service = new CheckoutApplicationService(cartReader, new FakeCatalogReader([]), new FakeCustomerReader(null), new FakeOrderRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<CheckoutException>(() => service.CheckoutAsync(new CheckoutRequest(cartId, customerId, Guid.NewGuid())));
    }
    [Fact]
    public async Task CheckoutShouldRejectCartOwnedByAnotherCustomer()
    {
        var cartId = Guid.NewGuid();
        var cartReader = new FakeCartReader(new CheckoutCart(cartId, Guid.NewGuid(), [new CheckoutCartItem(Guid.NewGuid(), 1)]));
        var service = new CheckoutApplicationService(cartReader, new FakeCatalogReader([]), new FakeCustomerReader(null), new FakeOrderRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<CheckoutException>(() => service.CheckoutAsync(new CheckoutRequest(cartId, Guid.NewGuid(), Guid.NewGuid())));
    }
    private sealed class FakeCartReader(CheckoutCart? cart) : ICheckoutCartReader
    {
        public Task<CheckoutCart?> GetAsync(Guid cartId, CancellationToken cancellationToken = default) => Task.FromResult(cart);
    }
    private sealed class FakeCatalogReader(IReadOnlyCollection<CheckoutCatalogItem> items) : ICheckoutCatalogReader
    {
        public Task<IReadOnlyCollection<CheckoutCatalogItem>> GetItemsAsync(IReadOnlyCollection<Guid> skuIds, CancellationToken cancellationToken = default) => Task.FromResult(items);
    }
    private sealed class FakeCustomerReader(CheckoutAddress? address) : ICheckoutCustomerReader
    {
        public Task<CheckoutAddress?> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default) => Task.FromResult(address);
    }
    private sealed class FakeOrderRepository : IOrderRepository
    {
        public Order? AddedOrder { get; private set; }

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            AddedOrder = order;
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
    }
    private static CheckoutApplicationService CreateService(CheckoutCart? cart = null, IReadOnlyCollection<CheckoutCatalogItem>? catalogItems = null, CheckoutAddress? address = null, FakeOrderRepository? repository = null, FakeUnitOfWork? unitOfWork = null)
    {
        repository ??= new FakeOrderRepository();
        unitOfWork ??= new FakeUnitOfWork();

        return new CheckoutApplicationService(
            new FakeCartReader(cart),
            new FakeCatalogReader(catalogItems ?? Array.Empty<CheckoutCatalogItem>()),
            new FakeCustomerReader(address),
            repository,
            unitOfWork);
    }
    private sealed class FakeUnitOfWork : IOrdersUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}