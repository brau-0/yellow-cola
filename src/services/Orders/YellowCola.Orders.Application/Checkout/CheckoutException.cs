namespace YellowCola.Orders.Application.Checkout;

public enum CheckoutFailureType
{
    NotFound,
    Conflict
}

public sealed class CheckoutException(string message, CheckoutFailureType failureType = CheckoutFailureType.Conflict) : Exception(message)
{
    public CheckoutFailureType FailureType { get; } = failureType;
}