using YellowCola.Inventory.Application.Reservations;
using YellowCola.Inventory.Domain.Reservations;

namespace YellowCola.Inventory.UnitTests;

public sealed class InventoryReservationTests
{
    [Fact]
    public void NewReservationShouldBeActive()
    {
        var reservation =
            CreateReservation();

        Assert.Equal(
            InventoryReservationStatus.Active,
            reservation.Status);

        Assert.True(
            reservation.IsActive);
    }

    [Fact]
    public void ReleaseShouldChangeStatusToReleased()
    {
        var reservation =
            CreateReservation();

        reservation.Release();

        Assert.Equal(
            InventoryReservationStatus.Released,
            reservation.Status);

        Assert.False(
            reservation.IsActive);
    }

    [Fact]
    public void ConsumeShouldChangeStatusToConsumed()
    {
        var reservation =
            CreateReservation();

        reservation.Consume();

        Assert.Equal(
            InventoryReservationStatus.Consumed,
            reservation.Status);
    }

    [Fact]
    public void ExpireShouldChangeStatusAfterExpiration()
    {
        var createdAt =
            DateTimeOffset.UtcNow;

        var reservation =
            CreateReservation(
                createdAt,
                createdAt.AddMinutes(15));

        reservation.Expire(
            createdAt.AddMinutes(16));

        Assert.Equal(
            InventoryReservationStatus.Expired,
            reservation.Status);
    }

    [Fact]
    public void ExpireBeforeExpirationShouldFail()
    {
        var createdAt =
            DateTimeOffset.UtcNow;

        var reservation =
            CreateReservation(
                createdAt,
                createdAt.AddMinutes(15));

        Action action = () =>
            reservation.Expire(
                createdAt.AddMinutes(10));

        Assert.Throws<InvalidOperationException>(
            action);
    }

    [Fact]
    public void FinalReservationShouldNotTransitionAgain()
    {
        var reservation =
            CreateReservation();

        reservation.Release();

        Action action =
            reservation.Consume;

        Assert.Throws<InvalidOperationException>(
            action);
    }
    

   
    private static InventoryReservation CreateReservation()
    {
        var createdAt = DateTimeOffset.UtcNow;
        return CreateReservation(createdAt, createdAt.AddMinutes(15));
    }

    private static InventoryReservation CreateReservation(DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        return new InventoryReservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            quantity: 2,
            createdAt,
            expiresAt);
    }   
}