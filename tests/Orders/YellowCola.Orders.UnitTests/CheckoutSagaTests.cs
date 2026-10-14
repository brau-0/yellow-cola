using YellowCola.Orders.Domain.Checkout;

namespace YellowCola.Orders.UnitTests;

public sealed class CheckoutSagaTests
{
    [Fact]
    public void NewSagaShouldStartInStartedStatus()
    {
        var saga = CreateSaga();

        Assert.Equal(CheckoutSagaStatus.Started, saga.Status);
    }

    [Fact]
    public void HappyPathShouldReachCompleted()
    {
        var now = DateTimeOffset.UtcNow;
        var saga = CreateSaga(now);

        saga.MarkInventoryReservationPending(now.AddSeconds(1));
        saga.MarkInventoryReserved(now.AddSeconds(2));
        saga.MarkPaymentAuthorizationPending(now.AddSeconds(3));
        saga.MarkPaymentAuthorized(now.AddSeconds(4));
        saga.Complete(now.AddSeconds(5));

        Assert.Equal(CheckoutSagaStatus.Completed, saga.Status);
    }

    [Fact]
    public void PaymentCannotBeRequestedBeforeInventoryIsReserved()
    {
        var saga = CreateSaga();

        Assert.Throws<InvalidOperationException>(() => saga.MarkPaymentAuthorizationPending(DateTimeOffset.UtcNow.AddSeconds(1)));
    }

    [Fact]
    public void SagaCannotCompleteBeforePaymentIsAuthorized()
    {
        var saga = CreateSaga();

        Assert.Throws<InvalidOperationException>(() => saga.Complete(DateTimeOffset.UtcNow.AddSeconds(1)));
    }

    [Fact]
    public void PaymentFailureShouldBeginInventoryCompensation()
    {
        var now = DateTimeOffset.UtcNow;
        var saga = CreateSaga(now);

        saga.MarkInventoryReservationPending(now.AddSeconds(1));
        saga.MarkInventoryReserved(now.AddSeconds(2));
        saga.MarkPaymentAuthorizationPending(now.AddSeconds(3));
        saga.MarkInventoryReleasePending("Payment declined.", now.AddSeconds(4));

        Assert.Equal(CheckoutSagaStatus.InventoryReleasePending, saga.Status);
        Assert.Equal("Payment declined.", saga.FailureReason);
    }

    private static CheckoutSaga CreateSaga(DateTimeOffset? createdAtUtc = null) => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), createdAtUtc ?? DateTimeOffset.UtcNow);
}