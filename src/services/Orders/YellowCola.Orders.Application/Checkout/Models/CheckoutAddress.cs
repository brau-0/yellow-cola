namespace YellowCola.Orders.Application.Checkout;

public sealed record CheckoutAddress(Guid Id, Guid CustomerId, string RecipientName, string PostalCode, 
    string State, string Municipality, string Neighborhood, string Street, string ExteriorNumber, string? InteriorNumber, string? Reference);