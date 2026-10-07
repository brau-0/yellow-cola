namespace YellowCola.Cart.Application.Carts;

public sealed class CartConflictException(string message, Exception? innerException = null) 
    : Exception(message, innerException);