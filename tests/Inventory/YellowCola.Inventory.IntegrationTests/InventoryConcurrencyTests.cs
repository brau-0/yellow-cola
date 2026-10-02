using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Testcontainers.PostgreSql;
using YellowCola.Inventory.Application.Inventory;
using YellowCola.Inventory.Application.Persistence;
using YellowCola.Inventory.Domain.InventoryItems;
using YellowCola.Inventory.Infrastructure.Persistence;
using YellowCola.Inventory.IntegrationTests.Infrastructure;

namespace YellowCola.Inventory.IntegrationTests;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification =
        "xUnit manages the test lifetime through IAsyncLifetime, and disposable resources are released in DisposeAsync.")]
public sealed class InventoryConcurrencyTests
    : IAsyncLifetime
{
    private const string WarehouseCode =
        "WH-MX-01";

    private readonly PostgreSqlContainer
        _postgresContainer =
            new PostgreSqlBuilder(
                "postgres:18-alpine")
            .WithDatabase(
                "yc_inventory_tests")
            .WithUsername(
                "yellowcola")
            .WithPassword(
                "yellowcola_test")
            .Build();

    private InventoryWebApplicationFactory?
        _factory;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory =
            new InventoryWebApplicationFactory(
                _postgresContainer
                    .GetConnectionString());

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        await dbContext.Database
            .MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }

    private async Task<Guid>
    SeedInventoryItemAsync(
        int onHand,
        int reserved = 0)
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            Guid.NewGuid();

        var item =
            new InventoryItem(
                Guid.NewGuid(),
                skuId,
                WarehouseCode,
                onHand,
                reserved);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        dbContext.InventoryItems.Add(
            item);

        await dbContext.SaveChangesAsync();

        return skuId;
    }

    [Fact]
    public async Task
    GetForUpdateShouldBlockAnotherTransaction()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            await SeedInventoryItemAsync(
                onHand: 10);

        await using var scopeA =
            _factory.Services.CreateAsyncScope();

        var repositoryA =
            scopeA.ServiceProvider
                .GetRequiredService<
                    IInventoryRepository>();

        var unitOfWorkA =
            scopeA.ServiceProvider
                .GetRequiredService<
                    IInventoryUnitOfWork>();

        await using var transactionA =
            await unitOfWorkA
                .BeginTransactionAsync();

        var itemA =
            await repositoryA
                .GetForUpdateAsync(
                    skuId,
                    WarehouseCode);

        Assert.NotNull(
            itemA);

        await using var scopeB =
            _factory.Services.CreateAsyncScope();

        var repositoryB =
            scopeB.ServiceProvider
                .GetRequiredService<
                    IInventoryRepository>();

        var unitOfWorkB =
            scopeB.ServiceProvider
                .GetRequiredService<
                    IInventoryUnitOfWork>();

        await using var transactionB =
            await unitOfWorkB
                .BeginTransactionAsync();

        var secondLockTask =
            repositoryB.GetForUpdateAsync(
                skuId,
                WarehouseCode);

        var completedTask =
            await Task.WhenAny(
                secondLockTask,
                Task.Delay(
                    TimeSpan.FromMilliseconds(500)));

        Assert.NotSame(
            secondLockTask,
            completedTask);

        await transactionA.CommitAsync();

        var itemB =
            await secondLockTask.WaitAsync(
                TimeSpan.FromSeconds(5));

        Assert.NotNull(
            itemB);

        await transactionB.RollbackAsync();
    }

    [Fact]
    public async Task
    ConcurrentReservationsShouldNotOversell()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            await SeedInventoryItemAsync(
                onHand: 1);

        var startSignal =
            new TaskCompletionSource(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

        var attemptA =
            ReserveAfterSignalAsync(
                skuId,
                startSignal.Task);

        var attemptB =
            ReserveAfterSignalAsync(
                skuId,
                startSignal.Task);

        startSignal.SetResult();

        var exceptions =
            await Task.WhenAll(
                attemptA,
                attemptB);

        Assert.Equal(
            1,
            exceptions.Count(
                exception =>
                    exception is null));

        Assert.Equal(
            1,
            exceptions.Count(
                exception =>
                    exception
                        is InvalidOperationException));

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var storedItem =
            await dbContext.InventoryItems
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.SkuId == skuId &&
                        item.WarehouseCode ==
                            WarehouseCode);

        Assert.Equal(
            1,
            storedItem.OnHand);

        Assert.Equal(
            1,
            storedItem.Reserved);

        Assert.Equal(
            0,
            storedItem.Available);
    }

    private async Task<Exception?>
    ReserveAfterSignalAsync(
        Guid skuId,
        Task startSignal)
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        await startSignal;

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryApplicationService>();

        return await Record.ExceptionAsync(
            async () =>
            {
                await service.ReserveAsync(
                    skuId,
                    WarehouseCode,
                    quantity: 1);
            });
    }
}