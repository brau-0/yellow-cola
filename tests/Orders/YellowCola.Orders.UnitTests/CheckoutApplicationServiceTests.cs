using YellowCola.Checkout.Contracts.Inventory;
using YellowCola.Checkout.Contracts.Messaging;
using YellowCola.Orders.Application.Checkout;
using YellowCola.Orders.Application.Messaging;
using YellowCola.Orders.Application.Orders;
using YellowCola.Orders.Application.Persistence;
using YellowCola.Orders.Domain.Checkout;
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
        var outboxWriter = new FakeOutboxWriter();
        var sagaRepository = new FakeCheckoutSagaRepository();

        var service = new CheckoutApplicationService(cartReader, catalogReader, customerReader, repository, sagaRepository, unitOfWork, outboxWriter);

        var result = await service.CheckoutAsync(new CheckoutRequest(cartId, customerId, addressId));

        Assert.NotNull(sagaRepository.AddedSaga);
        Assert.Equal(CheckoutSagaStatus.InventoryReservationPending, sagaRepository.AddedSaga.Status);

        var command = Assert.IsType<ReserveInventoryCommand>(outboxWriter.AddedMessage);

        Assert.Equal(result.Order.Id, command.OrderId);
        Assert.Equal(sagaRepository.AddedSaga.Id, command.CorrelationId);
        Assert.Null(command.CausationId);
        Assert.Equal("WH-MX-01", command.WarehouseCode);
        Assert.Equal(result.Order.Lines.Count, command.Lines.Count);
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
        var outboxWriter = new FakeOutboxWriter();
        var cartReader = new FakeCartReader(new CheckoutCart(cartId, null, [new CheckoutCartItem(Guid.NewGuid(), 1)]));
        var service = new CheckoutApplicationService(cartReader, new FakeCatalogReader([]), new FakeCustomerReader(null)
            , new FakeOrderRepository(), new FakeCheckoutSagaRepository(), new FakeUnitOfWork(), outboxWriter);

        await Assert.ThrowsAsync<CheckoutException>(() => service.CheckoutAsync(new CheckoutRequest(cartId, customerId, Guid.NewGuid())));
        //

    }
    [Fact]
    public async Task CheckoutShouldRejectCartOwnedByAnotherCustomer()
    {
        var cartId = Guid.NewGuid();
        var cartReader = new FakeCartReader(new CheckoutCart(cartId, Guid.NewGuid(), [new CheckoutCartItem(Guid.NewGuid(), 1)]));
        var service = new CheckoutApplicationService(cartReader, new FakeCatalogReader([]), new FakeCustomerReader(null)
            , new FakeOrderRepository(), new FakeCheckoutSagaRepository(), new FakeUnitOfWork(), new FakeOutboxWriter());

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
    private static CheckoutApplicationService CreateService(
     CheckoutCart? cart = null,
     IReadOnlyCollection<CheckoutCatalogItem>? catalogItems = null,
     CheckoutAddress? address = null,
     FakeOrderRepository? repository = null,
     FakeCheckoutSagaRepository? sagaRepository = null,
     FakeUnitOfWork? unitOfWork = null,
     FakeOutboxWriter? outboxWriter = null)
    {
        repository ??= new FakeOrderRepository();
        sagaRepository ??= new FakeCheckoutSagaRepository();
        unitOfWork ??= new FakeUnitOfWork();
        outboxWriter ??= new FakeOutboxWriter();

        return new CheckoutApplicationService(new FakeCartReader(cart), new FakeCatalogReader(catalogItems ?? [])
            , new FakeCustomerReader(address), repository, sagaRepository, unitOfWork, outboxWriter);
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
    private sealed class FakeCheckoutSagaRepository : ICheckoutSagaRepository
    {
        public CheckoutSaga? AddedSaga { get; private set; }

        public Task AddAsync(CheckoutSaga saga, CancellationToken cancellationToken = default)
        {
            AddedSaga = saga;
            return Task.CompletedTask;
        }

        public Task<CheckoutSaga?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default) => Task.FromResult<CheckoutSaga?>(null);

        public Task<CheckoutSaga?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult<CheckoutSaga?>(null);
    }
    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public IntegrationMessage? AddedMessage { get; private set; }

        public void Add(IntegrationMessage message) => AddedMessage = message;
    }
}