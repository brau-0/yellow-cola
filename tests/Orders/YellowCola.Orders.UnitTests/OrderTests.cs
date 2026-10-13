using YellowCola.Orders.Domain.Orders;

namespace YellowCola.Orders.UnitTests;

public sealed class OrderTests
{
    [Fact]
    public void NewOrderShouldCreatePendingSnapshot()
    {
        var customerId = Guid.NewGuid();
        var address = CreateAddress();
        var lines = new[] { new OrderLine(Guid.NewGuid(), "YC-001", "Yellow Cola Original 6 Pack", 129.90m, 2, "MXN") };

        var order = new Order(Guid.NewGuid(), customerId, address, lines, DateTimeOffset.UtcNow);

        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(259.80m, order.TotalAmount);
        Assert.Equal("MXN", order.Currency);
        Assert.Single(order.Lines);
    }
    [Fact]
    public void OrderMustContainAtLeastOneLine()
    {
        Assert.Throws<ArgumentException>(() => _ = new Order(Guid.NewGuid(), Guid.NewGuid(), CreateAddress(), [], DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ConfirmShouldMovePendingOrderToConfirmed()
    {
        var order = CreateOrder();

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void ConfirmShouldBeIdempotent()
    {
        var order = CreateOrder();

        order.Confirm();
        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void CancelShouldMovePendingOrderToCancelled()
    {
        var order = CreateOrder();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void CancelConfirmedOrderShouldFail()
    {
        var order = CreateOrder();
        order.Confirm();

        Action action = order.Cancel;

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void ConfirmCancelledOrderShouldFail()
    {
        var order = CreateOrder();
        order.Cancel();

        Action action = order.Confirm;

        Assert.Throws<InvalidOperationException>(action);
    }
    

    [Fact]
    public void NewOrderShouldBePending()
    {
        var order = CreateOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
    }
    [Fact]
    public void CancelShouldBeIdempotent()
    {
        var order = CreateOrder();

        order.Cancel();
        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }
    [Fact]
    public void LineTotalShouldMultiplyPriceByQuantity()
    {
        var line = new OrderLine(Guid.NewGuid(), "YC-001", "Yellow Cola Original", 129.90m, 3, "MXN");

        Assert.Equal(389.70m, line.LineTotal);
    }
    [Fact]
    public void OrderLineShouldRejectNonPositiveQuantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new OrderLine(Guid.NewGuid(), "YC-001", "Yellow Cola Original", 129.90m, 0, "MXN"));
    }
    [Fact]
    public void OrderLineShouldRejectUnsupportedCurrency()
    {
        Assert.Throws<ArgumentException>(() => _ = new OrderLine(Guid.NewGuid(), "YC-001", "Yellow Cola Original", 129.90m, 1, "USD"));
    }
    [Fact]
    public void OrderTotalShouldBeCalculatedFromOrderLineSnapshots()
    {
        var lines = new[]
        {
            new OrderLine(Guid.NewGuid(), "YC-001", "Yellow Cola Original", 100m, 2, "MXN"),
            new OrderLine(Guid.NewGuid(), "SN-001", "Sunny Orange", 50m, 3, "MXN")
        };

        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), CreateAddress(), lines, DateTimeOffset.UtcNow);

        Assert.Equal(350m, order.TotalAmount);
    }
    [Fact]
    public void OrderShouldRejectDuplicateSkuLines()
    {
        var skuId = Guid.NewGuid();

        var lines = new[]
        {
        new OrderLine(skuId, "YC-001", "Yellow Cola Original", 100m, 1, "MXN"),
        new OrderLine(skuId, "YC-001", "Yellow Cola Original", 100m, 2, "MXN")
    };

        Assert.Throws<ArgumentException>(() => _ = new Order(Guid.NewGuid(), Guid.NewGuid(), CreateAddress(), lines, DateTimeOffset.UtcNow));
    }

    private static Order CreateOrder() => new(Guid.NewGuid(), Guid.NewGuid(), CreateAddress(), [new OrderLine(Guid.NewGuid(), "YC-001", "Yellow Cola Original 6 Pack", 129.90m, 2, "MXN")], DateTimeOffset.UtcNow);

    private static ShippingAddressSnapshot CreateAddress() => new(Guid.NewGuid(), "Braulio Doe", "00011", "CDMX", "Miguel Hidalgo", "Centro", "Av. Principal", "100", null, "Casa blanca");
}