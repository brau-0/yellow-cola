using System.Net;
using System.Net.Http.Json;

using YellowCola.Orders.Application.Checkout;

namespace YellowCola.Orders.Infrastructure.Checkout;

public sealed class HttpCheckoutCustomerReader(HttpClient httpClient) : ICheckoutCustomerReader
{
    public async Task<CheckoutAddress?> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id cannot be empty.", nameof(customerId));
        if (addressId == Guid.Empty) throw new ArgumentException("Address id cannot be empty.", nameof(addressId));

        using var response = await httpClient.GetAsync($"api/customers/{customerId:D}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();

        var customer = await response.Content.ReadFromJsonAsync<CustomerApiResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Customer API returned an empty response.");

        var address = customer.Addresses.SingleOrDefault(item => item.Id == addressId);

        if (address is null) return null;

        return new CheckoutAddress(
            address.Id,
            customer.Id,
            address.RecipientName,
            address.PostalCode,
            address.State,
            address.Municipality,
            address.Neighborhood,
            address.Street,
            address.ExteriorNumber,
            address.InteriorNumber,
            address.Reference);
    }
}