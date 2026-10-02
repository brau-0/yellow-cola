using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using YellowCola.Inventory.Application.Inventory;
using YellowCola.Inventory.Application.Persistence;
using YellowCola.Inventory.Application.Reservations;
using YellowCola.Inventory.Domain.InventoryItems;
using YellowCola.Inventory.Domain.Reservations;
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
                    new ReserveInventoryCommand(
                        Guid.NewGuid(),
                        skuId,
                        WarehouseCode,
                        Quantity: 1));
            });
    }
    [Fact]
    public async Task
    ReserveShouldPersistInventoryAndReservationAtomically()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            await SeedInventoryItemAsync(
                onHand: 10);

        var orderId =
            Guid.NewGuid();

        ReserveInventoryResult? result;

        await using (var scope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                scope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            result =
                await service.ReserveAsync(
                    new ReserveInventoryCommand(
                        orderId,
                        skuId,
                        WarehouseCode,
                        Quantity: 3));
        }

        Assert.NotNull(
            result);

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
                        item.SkuId == skuId);

        var storedReservation =
            await dbContext.InventoryReservations
                .AsNoTracking()
                .SingleAsync(
                    reservation =>
                        reservation.OrderId ==
                            orderId);

        Assert.Equal(
            3,
            storedItem.Reserved);

        Assert.Equal(
            7,
            storedItem.Available);

        Assert.Equal(
            3,
            storedReservation.Quantity);

        Assert.Equal(
            InventoryReservationStatus.Active,
            storedReservation.Status);

        Assert.Equal(
            storedItem.Id,
            storedReservation.InventoryItemId);
    }
    [Fact]
    public async Task
    FailedReservationInsertShouldRollbackInventoryChange()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            await SeedInventoryItemAsync(
                onHand: 10);

        var orderId =
            Guid.NewGuid();

        await using (var firstScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                firstScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            await service.ReserveAsync(
                new ReserveInventoryCommand(
                    orderId,
                    skuId,
                    WarehouseCode,
                    Quantity: 2));
        }

        await using (var secondScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                secondScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            await Assert.ThrowsAsync<
                DbUpdateException>(
                async () =>
                {
                    await service.ReserveAsync(
                        new ReserveInventoryCommand(
                            orderId,
                            skuId,
                            WarehouseCode,
                            Quantity: 1));
                });
        }

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
                        item.SkuId == skuId);

        var reservations =
            await dbContext.InventoryReservations
                .AsNoTracking()
                .Where(
                    reservation =>
                        reservation.OrderId ==
                            orderId)
                .ToListAsync();

        Assert.Equal(
            2,
            storedItem.Reserved);

        Assert.Equal(
            8,
            storedItem.Available);

        Assert.Single(
            reservations);

        Assert.Equal(
            2,
            reservations[0].Quantity);
    }

    [Fact]
    public async Task
    ReleaseShouldReturnReservedInventoryToAvailability()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            await SeedInventoryItemAsync(
                onHand: 10);

        var orderId =
            Guid.NewGuid();

        Guid reservationId;

        await using (var reserveScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                reserveScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            var reserveResult =
                await service.ReserveAsync(
                    new ReserveInventoryCommand(
                        orderId,
                        skuId,
                        WarehouseCode,
                        Quantity: 3));

            Assert.NotNull(
                reserveResult);

            reservationId =
                reserveResult.ReservationId;
        }

        await using (var releaseScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                releaseScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            var result =
                await service.ReleaseReservationAsync(
                    reservationId);

            Assert.NotNull(
                result);

            Assert.Equal(
                InventoryReservationStatus.Released,
                result.Status);
        }

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var inventoryItem =
            await dbContext.InventoryItems
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.SkuId == skuId);

        var reservation =
            await dbContext.InventoryReservations
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id == reservationId);

        Assert.Equal(
            10,
            inventoryItem.OnHand);

        Assert.Equal(
            0,
            inventoryItem.Reserved);

        Assert.Equal(
            10,
            inventoryItem.Available);

        Assert.Equal(
            InventoryReservationStatus.Released,
            reservation.Status);
    }
    [Fact]
    public async Task
    ConsumeShouldReduceOnHandAndReservedInventory()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            await SeedInventoryItemAsync(
                onHand: 10);

        var orderId =
            Guid.NewGuid();

        Guid reservationId;

        await using (var reserveScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                reserveScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            var reserveResult =
                await service.ReserveAsync(
                    new ReserveInventoryCommand(
                        orderId,
                        skuId,
                        WarehouseCode,
                        Quantity: 3));

            Assert.NotNull(
                reserveResult);

            reservationId =
                reserveResult.ReservationId;
        }

        await using (var consumeScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                consumeScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            var result =
                await service.ConsumeReservationAsync(
                    reservationId);

            Assert.NotNull(
                result);

            Assert.Equal(
                InventoryReservationStatus.Consumed,
                result.Status);
        }

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var inventoryItem =
            await dbContext.InventoryItems
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.SkuId == skuId);

        Assert.Equal(
            7,
            inventoryItem.OnHand);

        Assert.Equal(
            0,
            inventoryItem.Reserved);

        Assert.Equal(
            7,
            inventoryItem.Available);
    }

    [Fact]
    public async Task
    ExpireShouldReturnReservedInventoryToAvailability()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var seeded =
            await SeedExpiredReservationAsync(
                onHand: 10,
                quantity: 3);

        await using (var expireScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                expireScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            var result =
                await service.ExpireReservationAsync(
                    seeded.ReservationId);

            Assert.NotNull(
                result);

            Assert.Equal(
                InventoryReservationStatus.Expired,
                result.Status);
        }

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var inventoryItem =
            await dbContext.InventoryItems
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.SkuId ==
                            seeded.SkuId);

        Assert.Equal(
            10,
            inventoryItem.OnHand);

        Assert.Equal(
            0,
            inventoryItem.Reserved);

        Assert.Equal(
            10,
            inventoryItem.Available);
    }

    [Fact]
    public async Task
    ReleasedReservationShouldNotBeConsumed()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            await SeedInventoryItemAsync(
                onHand: 10);

        Guid reservationId;

        await using (var reserveScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                reserveScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            var reserveResult =
                await service.ReserveAsync(
                    new ReserveInventoryCommand(
                        Guid.NewGuid(),
                        skuId,
                        WarehouseCode,
                        Quantity: 3));

            Assert.NotNull(
                reserveResult);

            reservationId =
                reserveResult.ReservationId;
        }

        await using (var releaseScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                releaseScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            await service.ReleaseReservationAsync(
                reservationId);
        }

        await using (var consumeScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                consumeScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            await Assert.ThrowsAsync<
                InvalidOperationException>(
                async () =>
                {
                    await service
                        .ConsumeReservationAsync(
                            reservationId);
                });
        }

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var inventoryItem =
            await dbContext.InventoryItems
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.SkuId == skuId);

        var reservation =
            await dbContext.InventoryReservations
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id == reservationId);

        Assert.Equal(
            10,
            inventoryItem.OnHand);

        Assert.Equal(
            0,
            inventoryItem.Reserved);

        Assert.Equal(
            InventoryReservationStatus.Released,
            reservation.Status);
    }

    private async Task<(Guid SkuId, Guid ReservationId)>
    SeedExpiredReservationAsync(
        int onHand,
        int quantity)
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            Guid.NewGuid();

        var inventoryItem =
            new InventoryItem(
                Guid.NewGuid(),
                skuId,
                WarehouseCode,
                onHand,
                reserved: quantity);

        var now =
            DateTimeOffset.UtcNow;

        var reservation =
            new InventoryReservation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                inventoryItem.Id,
                quantity,
                now.AddMinutes(-30),
                now.AddMinutes(-15));

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        dbContext.InventoryItems.Add(
            inventoryItem);

        dbContext.InventoryReservations.Add(
            reservation);

        await dbContext.SaveChangesAsync();

        return (
            skuId,
            reservation.Id);
    }
    [Fact]
    public async Task
    ReleasingSameReservationTwiceShouldBeIdempotent()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            await SeedInventoryItemAsync(
                onHand: 10);

        Guid reservationId;

        await using (var reserveScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                reserveScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            var reserveResult =
                await service.ReserveAsync(
                    new ReserveInventoryCommand(
                        Guid.NewGuid(),
                        skuId,
                        WarehouseCode,
                        Quantity: 3));

            Assert.NotNull(
                reserveResult);

            reservationId =
                reserveResult.ReservationId;
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var scope =
                _factory.Services.CreateAsyncScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            var result =
                await service.ReleaseReservationAsync(
                    reservationId);

            Assert.NotNull(
                result);

            Assert.Equal(
                InventoryReservationStatus.Released,
                result.Status);
        }

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var item =
            await dbContext.InventoryItems
                .AsNoTracking()
                .SingleAsync(
                    inventoryItem =>
                        inventoryItem.SkuId == skuId);

        Assert.Equal(
            10,
            item.OnHand);

        Assert.Equal(
            0,
            item.Reserved);
    }

    [Fact]
    public async Task
    ConcurrentDuplicateConsumeShouldOnlyConsumeOnce()
    {
        ArgumentNullException.ThrowIfNull(
            _factory);

        var skuId =
            await SeedInventoryItemAsync(
                onHand: 10);

        Guid reservationId;

        await using (var reserveScope =
            _factory.Services.CreateAsyncScope())
        {
            var service =
                reserveScope.ServiceProvider
                    .GetRequiredService<
                        InventoryApplicationService>();

            var reserveResult =
                await service.ReserveAsync(
                    new ReserveInventoryCommand(
                        Guid.NewGuid(),
                        skuId,
                        WarehouseCode,
                        Quantity: 3));

            Assert.NotNull(
                reserveResult);

            reservationId =
                reserveResult.ReservationId;
        }

        var startSignal =
            new TaskCompletionSource(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

        var attemptA =
            ConsumeAfterSignalAsync(
                reservationId,
                startSignal.Task);

        var attemptB =
            ConsumeAfterSignalAsync(
                reservationId,
                startSignal.Task);

        startSignal.SetResult();

        var results =
            await Task.WhenAll(
                attemptA,
                attemptB);

        Assert.All(
            results,
            result =>
            {
                Assert.NotNull(
                    result);

                Assert.Equal(
                    InventoryReservationStatus.Consumed,
                    result.Status);
            });

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<
                    InventoryDbContext>();

        var inventoryItem =
            await dbContext.InventoryItems
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.SkuId == skuId);

        var reservation =
            await dbContext.InventoryReservations
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id == reservationId);

        Assert.Equal(
            7,
            inventoryItem.OnHand);

        Assert.Equal(
            0,
            inventoryItem.Reserved);

        Assert.Equal(
            InventoryReservationStatus.Consumed,
            reservation.Status);
    }

    private async Task<ReservationTransitionResult?> ConsumeAfterSignalAsync(Guid reservationId, Task startSignal)
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

        return await service
            .ConsumeReservationAsync(
                reservationId);
    }


}