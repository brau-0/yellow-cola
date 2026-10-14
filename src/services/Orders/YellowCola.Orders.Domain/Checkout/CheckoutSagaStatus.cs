namespace YellowCola.Orders.Domain.Checkout;

public enum CheckoutSagaStatus
{
    Started = 1,
    InventoryReservationPending = 2,
    InventoryReserved = 3,
    PaymentAuthorizationPending = 4,
    PaymentAuthorized = 5,
    InventoryReleasePending = 6,
    Completed = 7,
    Failed = 8
}