using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using YellowCola.Checkout.Contracts.Inventory;
using YellowCola.Inventory.Application.Messaging;
using YellowCola.Inventory.Domain.InventoryItems;
using YellowCola.Inventory.Infrastructure.Persistence;
using YellowCola.Inventory.IntegrationTests.Infrastructure;

using IntegrationReserveInventoryCommand = YellowCola.Checkout.Contracts.Inventory.ReserveInventoryCommand;

namespace YellowCola.Inventory.IntegrationTests;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "xUnit manages the test lifetime through IAsyncLifetime, and disposable resources are released in DisposeAsync.")]
public sealed class InventoryMessageHandlingTests : IAsyncLifetime
{
    private const string WarehouseCode = "WH-MX-01";

    private readonly PostgreSqlContainer _postgresContainer =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("yc_inventory_message_tests")
            .WithUsername("yellowcola")
            .WithPassword("yellowcola_test")
            .Build();

    private InventoryWebApplicationFactory? _factory;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new InventoryWebApplicationFactory(_postgresContainer.GetConnectionString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task DuplicateMessageShouldReserveInventoryOnlyOnce()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var skuId = await SeedInventoryItemAsync(onHand: 10);
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var command = new IntegrationReserveInventoryCommand(
            messageId,
            correlationId,
            null,
            DateTimeOffset.UtcNow,
            orderId,
            WarehouseCode,
            [new ReserveInventoryLine(skuId, 3)]);

        InventoryMessageHandlingOutcome firstResult;

        await using (var firstScope = _factory.Services.CreateAsyncScope())
        {
            var handler = firstScope.ServiceProvider.GetRequiredService<ReserveInventoryMessageHandler>();
            firstResult = await handler.HandleAsync(command);
        }

        InventoryMessageHandlingOutcome secondResult;

        await using (var secondScope = _factory.Services.CreateAsyncScope())
        {
            var handler = secondScope.ServiceProvider.GetRequiredService<ReserveInventoryMessageHandler>();
            secondResult = await handler.HandleAsync(command);
        }

        Assert.Equal(InventoryMessageHandlingOutcome.Processed, firstResult);
        Assert.Equal(InventoryMessageHandlingOutcome.Duplicate, secondResult);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var storedItem = await dbContext.InventoryItems
            .AsNoTracking()
            .SingleAsync(item => item.SkuId == skuId && item.WarehouseCode == WarehouseCode);

        var reservations = await dbContext.InventoryReservations
            .AsNoTracking()
            .Where(reservation => reservation.OrderId == orderId)
            .ToListAsync();

        Assert.Equal(10, storedItem.OnHand);
        Assert.Equal(3, storedItem.Reserved);
        Assert.Equal(7, storedItem.Available);

        Assert.Single(reservations);
        Assert.Equal(3, reservations[0].Quantity);

        Assert.Equal(1, await CountInboxMessagesAsync(dbContext, messageId));
        Assert.Equal(1, await CountOutboxMessagesByCausationIdAsync(dbContext, messageId));
        Assert.True(await IsInboxMessageProcessedAsync(dbContext, messageId));
    }

    [Fact]
    public async Task MultiSkuReservationShouldReserveAllLinesAtomically()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var skuA = await SeedInventoryItemAsync(onHand: 10);
        var skuB = await SeedInventoryItemAsync(onHand: 8);

        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var command = new IntegrationReserveInventoryCommand(
            messageId,
            correlationId,
            null,
            DateTimeOffset.UtcNow,
            orderId,
            WarehouseCode,
            [
                new ReserveInventoryLine(skuA, 3),
                new ReserveInventoryLine(skuB, 4)
            ]);

        InventoryMessageHandlingOutcome result;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ReserveInventoryMessageHandler>();
            result = await handler.HandleAsync(command);
        }

        Assert.Equal(InventoryMessageHandlingOutcome.Processed, result);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var items = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(item => item.SkuId == skuA || item.SkuId == skuB)
            .ToListAsync();

        var itemA = items.Single(item => item.SkuId == skuA);
        var itemB = items.Single(item => item.SkuId == skuB);

        var reservations = await dbContext.InventoryReservations
            .AsNoTracking()
            .Where(reservation => reservation.OrderId == orderId)
            .ToListAsync();

        Assert.Equal(3, itemA.Reserved);
        Assert.Equal(7, itemA.Available);

        Assert.Equal(4, itemB.Reserved);
        Assert.Equal(4, itemB.Available);

        Assert.Equal(2, reservations.Count);

        Assert.Equal(
            "checkout.inventory.inventory-reserved",
            await GetOutboxMessageTypeByCausationIdAsync(dbContext, messageId));

        Assert.True(await IsInboxMessageProcessedAsync(dbContext, messageId));
    }

    [Fact]
    public async Task InsufficientStockShouldNotPartiallyReserveOrder()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var skuA = await SeedInventoryItemAsync(onHand: 10);
        var skuB = await SeedInventoryItemAsync(onHand: 1);

        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var command = new IntegrationReserveInventoryCommand(
            messageId,
            correlationId,
            null,
            DateTimeOffset.UtcNow,
            orderId,
            WarehouseCode,
            [
                new ReserveInventoryLine(skuA, 3),
                new ReserveInventoryLine(skuB, 5)
            ]);

        InventoryMessageHandlingOutcome result;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ReserveInventoryMessageHandler>();
            result = await handler.HandleAsync(command);
        }

        Assert.Equal(InventoryMessageHandlingOutcome.Processed, result);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var items = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(item => item.SkuId == skuA || item.SkuId == skuB)
            .ToListAsync();

        var itemA = items.Single(item => item.SkuId == skuA);
        var itemB = items.Single(item => item.SkuId == skuB);

        var reservations = await dbContext.InventoryReservations
            .AsNoTracking()
            .Where(reservation => reservation.OrderId == orderId)
            .ToListAsync();

        Assert.Equal(0, itemA.Reserved);
        Assert.Equal(10, itemA.Available);

        Assert.Equal(0, itemB.Reserved);
        Assert.Equal(1, itemB.Available);

        Assert.Empty(reservations);

        Assert.Equal(
            "checkout.inventory.inventory-reservation-failed",
            await GetOutboxMessageTypeByCausationIdAsync(dbContext, messageId));

        Assert.True(await IsInboxMessageProcessedAsync(dbContext, messageId));
    }

    [Fact]
    public async Task MissingSkuShouldProduceFailureEventWithoutReservations()
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var existingSkuId = await SeedInventoryItemAsync(onHand: 10);
        var missingSkuId = Guid.NewGuid();

        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var command = new IntegrationReserveInventoryCommand(
            messageId,
            correlationId,
            null,
            DateTimeOffset.UtcNow,
            orderId,
            WarehouseCode,
            [
                new ReserveInventoryLine(existingSkuId, 2),
                new ReserveInventoryLine(missingSkuId, 1)
            ]);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ReserveInventoryMessageHandler>();
            var result = await handler.HandleAsync(command);

            Assert.Equal(InventoryMessageHandlingOutcome.Processed, result);
        }

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var storedItem = await dbContext.InventoryItems
            .AsNoTracking()
            .SingleAsync(item => item.SkuId == existingSkuId);

        var reservations = await dbContext.InventoryReservations
            .AsNoTracking()
            .Where(reservation => reservation.OrderId == orderId)
            .ToListAsync();

        Assert.Equal(0, storedItem.Reserved);
        Assert.Equal(10, storedItem.Available);
        Assert.Empty(reservations);

        Assert.Equal(
            "checkout.inventory.inventory-reservation-failed",
            await GetOutboxMessageTypeByCausationIdAsync(dbContext, messageId));

        Assert.True(await IsInboxMessageProcessedAsync(dbContext, messageId));
    }

    private async Task<Guid> SeedInventoryItemAsync(int onHand, int reserved = 0)
    {
        ArgumentNullException.ThrowIfNull(_factory);

        var skuId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(Guid.NewGuid(), skuId, WarehouseCode, onHand, reserved);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        dbContext.InventoryItems.Add(inventoryItem);
        await dbContext.SaveChangesAsync();

        return skuId;
    }

    private static async Task<long> CountInboxMessagesAsync(InventoryDbContext dbContext, Guid messageId)
    {
        await using var command = CreateCommand(
            dbContext,
            "SELECT COUNT(*) FROM inbox_messages WHERE message_id = @messageId;",
            ("messageId", messageId));

        var result = await command.ExecuteScalarAsync(cancellationToken: CancellationToken.None);

        return result is long count ? count : throw new InvalidOperationException("COUNT(*) did not return an Int64 value.");
    }

    private static async Task<long> CountOutboxMessagesByCausationIdAsync(InventoryDbContext dbContext, Guid causationId)
    {
        await using var command = CreateCommand(
            dbContext,
            "SELECT COUNT(*) FROM outbox_messages WHERE causation_id = @causationId;",
            ("causationId", causationId));

        var result = await command.ExecuteScalarAsync(cancellationToken: CancellationToken.None);

        return result is long count ? count : throw new InvalidOperationException("COUNT(*) did not return an Int64 value.");
    }

    private static async Task<string?> GetOutboxMessageTypeByCausationIdAsync(InventoryDbContext dbContext, Guid causationId)
    {
        await using var command = CreateCommand(
            dbContext,
            "SELECT message_type FROM outbox_messages WHERE causation_id = @causationId LIMIT 1;",
            ("causationId", causationId));

        var result = await command.ExecuteScalarAsync();

        return result is string value ? value : null;
    }

    private static async Task<bool> IsInboxMessageProcessedAsync(InventoryDbContext dbContext, Guid messageId)
    {
        await using var command = CreateCommand(
            dbContext,
            "SELECT processed_at_utc IS NOT NULL FROM inbox_messages WHERE message_id = @messageId;",
            ("messageId", messageId));

        var result = await command.ExecuteScalarAsync();

        return result is bool value && value;
    }

    private static DbCommand CreateCommand(InventoryDbContext dbContext, string commandText, params (string Name, object Value)[] parameters)
    {
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open) connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = commandText;

        foreach (var parameterValue in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterValue.Name;
            parameter.Value = parameterValue.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }
}