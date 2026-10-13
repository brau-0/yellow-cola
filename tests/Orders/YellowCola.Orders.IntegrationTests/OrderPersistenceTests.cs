using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using YellowCola.Orders.Application.Orders;
using YellowCola.Orders.Application.Persistence;
using YellowCola.Orders.Domain.Orders;
using YellowCola.Orders.Infrastructure.Persistence;
using YellowCola.Orders.IntegrationTests.Infrastructure;

namespace YellowCola.Orders.IntegrationTests;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "xUnit manages the test lifetime through IAsyncLifetime.")]
public sealed class OrderPersistenceTests : IAsyncLifetime
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
    public async Task SaveAndReloadShouldRoundTripCompleteAggregate()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var order = CreateOrder();

        using (var writeScope = _factory.Services.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IOrdersUnitOfWork>();

            await repository.AddAsync(order);
            await unitOfWork.SaveChangesAsync();
        }

        using (var readScope = _factory.Services.CreateScope())
        {
            var repository = readScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var restored = await repository.GetByIdAsync(order.Id);

            Assert.NotNull(restored);
            Assert.Equal(order.Id, restored.Id);
            Assert.Equal(order.CustomerId, restored.CustomerId);
            Assert.Equal(OrderStatus.Pending, restored.Status);
            Assert.Equal("MXN", restored.Currency);
            Assert.Equal(order.TotalAmount, restored.TotalAmount);
            Assert.Equal(2, restored.Lines.Count);

            Assert.Equal(order.ShippingAddress.SourceAddressId, restored.ShippingAddress.SourceAddressId);
            Assert.Equal(order.ShippingAddress.PostalCode, restored.ShippingAddress.PostalCode);
            Assert.Equal(order.ShippingAddress.Street, restored.ShippingAddress.Street);
        }
    }
    [Fact]
    public async Task RehydratedOrderShouldPreserveDomainBehavior()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var order = CreateOrder();

        using (var writeScope = _factory.Services.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IOrdersUnitOfWork>();

            await repository.AddAsync(order);
            await unitOfWork.SaveChangesAsync();
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var repository = updateScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var unitOfWork = updateScope.ServiceProvider.GetRequiredService<IOrdersUnitOfWork>();

            var restored = await repository.GetByIdAsync(order.Id);

            Assert.NotNull(restored);

            restored.Confirm();

            await unitOfWork.SaveChangesAsync();
        }

        using (var verificationScope = _factory.Services.CreateScope())
        {
            var repository = verificationScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var restored = await repository.GetByIdAsync(order.Id);

            Assert.NotNull(restored);
            Assert.Equal(OrderStatus.Confirmed, restored.Status);
            Assert.Throws<InvalidOperationException>(restored.Cancel);
        }
    }
    [Fact]
    public async Task PersistedOrderShouldRetainHistoricalSnapshots()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var order = CreateOrder();

        using (var writeScope = _factory.Services.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var unitOfWork = writeScope.ServiceProvider.GetRequiredService<IOrdersUnitOfWork>();

            await repository.AddAsync(order);
            await unitOfWork.SaveChangesAsync();
        }

        using var readScope = _factory.Services.CreateScope();
        var readRepository = readScope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var restored = await readRepository.GetByIdAsync(order.Id);

        Assert.NotNull(restored);

        var firstLine = restored.Lines.Single(line => line.SkuCode == "YC-001");

        Assert.Equal(129.90m, firstLine.UnitPrice);
        Assert.Equal("Yellow Cola Original 6 Pack", firstLine.Description);
        Assert.Equal("Av. Principal", restored.ShippingAddress.Street);
        Assert.Equal("00011", restored.ShippingAddress.PostalCode);
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