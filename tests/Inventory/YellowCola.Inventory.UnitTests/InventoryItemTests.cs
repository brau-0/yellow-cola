

using YellowCola.Inventory.Domain.InventoryItems;

namespace YellowCola.Inventory.UnitTests;

public sealed class InventoryItemTests
{
    [Fact]
    public void AvailableShouldEqualOnHandMinusReserved()
    {
        var item =
            CreateInventoryItem(
                onHand: 100,
                reserved: 20);

        Assert.Equal(
            80,
            item.Available);
    }

    [Fact]
    public void ReserveShouldIncreaseReservedAndReduceAvailable()
    {
        var item =
            CreateInventoryItem(
                onHand: 100,
                reserved: 20);

        item.Reserve(30);

        Assert.Equal(
            50,
            item.Reserved);

        Assert.Equal(
            50,
            item.Available);
    }

    [Fact]
    public void ReserveMoreThanAvailableShouldFail()
    {
        var item =
            CreateInventoryItem(
                onHand: 10,
                reserved: 8);

        Action action = () =>
            item.Reserve(3);

        Assert.Throws<InvalidOperationException>(
            action);
    }

    [Fact]
    public void ReleaseShouldDecreaseReserved()
    {
        var item =
            CreateInventoryItem(
                onHand: 100,
                reserved: 30);

        item.Release(20);

        Assert.Equal(
            10,
            item.Reserved);

        Assert.Equal(
            90,
            item.Available);
    }

    [Fact]
    public void ReleaseMoreThanReservedShouldFail()
    {
        var item =
            CreateInventoryItem(
                onHand: 100,
                reserved: 10);

        Action action = () =>
            item.Release(11);

        Assert.Throws<InvalidOperationException>(
            action);
    }

    [Fact]
    public void ReceiveShouldIncreaseOnHand()
    {
        var item =
            CreateInventoryItem(
                onHand: 100,
                reserved: 20);

        item.Receive(25);

        Assert.Equal(
            125,
            item.OnHand);

        Assert.Equal(
            105,
            item.Available);
    }
    [Fact]
    public void ConsumeReservedShouldReduceOnHandAndReserved()
    {
        var item = CreateInventoryItem( onHand: 10,
            reserved: 3);

        item.ConsumeReserved(3);

        Assert.Equal(7, item.OnHand);

        Assert.Equal(0, item.Reserved);

        Assert.Equal(7, item.Available);
    }
    [Fact]
    public void ConsumeMoreThanReservedShouldFail()
    {
        var item = CreateInventoryItem(onHand: 10, reserved: 3);

        Action action = () => item.ConsumeReserved(4);

        Assert.Throws<InvalidOperationException>(action);
    }

    private static InventoryItem
        CreateInventoryItem(int onHand, int reserved)
    {
        return new InventoryItem(Guid.NewGuid(), Guid.NewGuid(), "WH-MX-01", onHand, reserved);
    }
}