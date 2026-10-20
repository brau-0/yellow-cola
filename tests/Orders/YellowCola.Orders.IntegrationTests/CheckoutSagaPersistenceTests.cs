using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using YellowCola.Checkout.Contracts.Inventory;
using YellowCola.Orders.Application.Checkout;
using YellowCola.Orders.Application.Messaging;
using YellowCola.Orders.Application.Orders;
using YellowCola.Orders.Application.Persistence;
using YellowCola.Orders.Domain.Checkout;
using YellowCola.Orders.Domain.Orders;
using YellowCola.Orders.Infrastructure.Persistence;
using YellowCola.Orders.IntegrationTests.Infrastructure;

namespace YellowCola.Orders.IntegrationTests;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "xUnit manages the test lifetime through IAsyncLifetime.")]

public sealed class CheckoutSagaPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:18-alpine")
    .WithDatabase("yc_orders")
    .WithUsername("yellowcola")
    .WithPassword("yellowcola_dev_only")
    .Build();
    private OrdersWebApplicationFactory? _factory;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new OrdersWebApplicationFactory(_postgresContainer.GetConnectionString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        await dbContext.Database.MigrateAsync();
    }
    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
    [Fact]
    public async Task SaveAndReloadShouldRoundTripCheckoutSaga()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var order = CreateOrder();
        var saga = new CheckoutSaga(Guid.NewGuid(), order.Id, Guid.NewGuid(), order.CustomerId, order.CreatedAtUtc);

        using (var writeScope = _factory.Services.CreateScope())
        {
            var orderRepository = writeScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var sagaRepository = writeScope.ServiceProvider.GetRequiredService<ICheckoutSagaRepository>();
            var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IOrdersUnitOfWork>();

            await orderRepository.AddAsync(order);
            await sagaRepository.AddAsync(saga);
            await unitOfWork.SaveChangesAsync();
        }

        using (var readScope = _factory.Services.CreateScope())
        {
            var sagaRepository = readScope.ServiceProvider.GetRequiredService<ICheckoutSagaRepository>();
            var restored = await sagaRepository.GetByIdAsync(saga.Id);

            Assert.NotNull(restored);
            Assert.Equal(saga.Id, restored.Id);
            Assert.Equal(order.Id, restored.OrderId);
            Assert.Equal(saga.CartId, restored.CartId);
            Assert.Equal(order.CustomerId, restored.CustomerId);
            Assert.Equal(CheckoutSagaStatus.Started, restored.Status);
            Assert.Null(restored.FailureReason);
        }

    }

    [Fact]
    public async Task RehydratedSagaShouldPreserveStateMachineBehavior()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var order = CreateOrder();
        var saga = new CheckoutSaga(Guid.NewGuid(), order.Id, Guid.NewGuid(), order.CustomerId, order.CreatedAtUtc);

        using (var writeScope = _factory.Services.CreateScope())
        {
            var orderRepository = writeScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var sagaRepository = writeScope.ServiceProvider.GetRequiredService<ICheckoutSagaRepository>();
            var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IOrdersUnitOfWork>();

            await orderRepository.AddAsync(order);
            await sagaRepository.AddAsync(saga);
            await unitOfWork.SaveChangesAsync();
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var sagaRepository = updateScope.ServiceProvider.GetRequiredService<ICheckoutSagaRepository>();
            var unitOfWork = updateScope.ServiceProvider.GetRequiredService<IOrdersUnitOfWork>();

            var restored = await sagaRepository.GetByIdAsync(saga.Id);

            Assert.NotNull(restored);

            restored.MarkInventoryReservationPending(restored.UpdatedAtUtc.AddSeconds(1));

            await unitOfWork.SaveChangesAsync();
        }

        using (var verificationScope = _factory.Services.CreateScope())
        {
            var sagaRepository = verificationScope.ServiceProvider.GetRequiredService<ICheckoutSagaRepository>();
            var restored = await sagaRepository.GetByIdAsync(saga.Id);

            Assert.NotNull(restored);
            Assert.Equal(CheckoutSagaStatus.InventoryReservationPending, restored.Status);
        }
    }
    [Fact]
    public async Task OrderSagaAndOutboxShouldBePersistedTogether()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var order = CreateOrder();
        var saga = new CheckoutSaga(Guid.NewGuid(), order.Id, Guid.NewGuid(), order.CustomerId, order.CreatedAtUtc);

        saga.MarkInventoryReservationPending(order.CreatedAtUtc);

        var command = new ReserveInventoryCommand(
            Guid.NewGuid(),
            saga.Id,
            null,
            order.CreatedAtUtc,
            order.Id,
            "WH-MX-01",
            order.Lines.Select(line => new ReserveInventoryLine(line.SkuId, line.Quantity)).ToArray());

        using (var scope = _factory.Services.CreateScope())
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var sagaRepository = scope.ServiceProvider.GetRequiredService<ICheckoutSagaRepository>();
            var outboxWriter = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrdersUnitOfWork>();

            await orderRepository.AddAsync(order);
            await sagaRepository.AddAsync(saga);
            outboxWriter.Add(command);

            await unitOfWork.SaveChangesAsync();
        }

        using var verificationScope = _factory.Services.CreateScope();

        var orderRepositoryVerification = verificationScope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var sagaRepositoryVerification = verificationScope.ServiceProvider.GetRequiredService<ICheckoutSagaRepository>();

        var restoredOrder = await orderRepositoryVerification.GetByIdAsync(order.Id);
        var restoredSaga = await sagaRepositoryVerification.GetByIdAsync(saga.Id);

        Assert.NotNull(restoredOrder);
        Assert.NotNull(restoredSaga);
        Assert.Equal(CheckoutSagaStatus.InventoryReservationPending, restoredSaga.Status);
    }
    private static Order CreateOrder()
    {
        var address = new ShippingAddressSnapshot(Guid.NewGuid(), "Braulio Doe", "00011", "CDMX", "Miguel Hidalgo", "Centro", "Av. Principal", "100", null, "Casa blanca");

        var lines = new[]
        {
        new OrderLine(Guid.NewGuid(), "YC-001", "Yellow Cola Original 6 Pack", 129.90m, 2, "MXN"),
        new OrderLine(Guid.NewGuid(), "SN-001", "Sunny Orange", 59.50m, 3, "MXN")
    };

        return new Order(Guid.NewGuid(), Guid.NewGuid(), address, lines, DateTimeOffset.UtcNow);
    }
}
