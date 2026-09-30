namespace YellowCola.Customer.Application.Customers;

public sealed record AddAddressCommand(
    string Label,
    string RecipientName,
    string PostalCode,
    string State,
    string Municipality,
    string Neighborhood,
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string? Reference);