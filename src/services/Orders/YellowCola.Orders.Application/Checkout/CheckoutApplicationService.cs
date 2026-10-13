using YellowCola.Orders.Application.Orders;
using YellowCola.Orders.Application.Persistence;
using YellowCola.Orders.Domain.Orders;

namespace YellowCola.Orders.Application.Checkout;

public sealed class CheckoutApplicationService(ICheckoutCartReader cartReader, ICheckoutCatalogReader catalogReader
    , ICheckoutCustomerReader customerReader, IOrderRepository orderRepository
    , IOrdersUnitOfWork unitOfWork)
{
    public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var cart = await cartReader.GetAsync(request.CartId, cancellationToken)
     ?? throw new CheckoutException($"Cart '{request.CartId}' was not found.", CheckoutFailureType.NotFound);

        if (!cart.CustomerId.HasValue) throw new CheckoutException("Anonymous cart must be assigned to a customer before checkout.");
        if (cart.CustomerId.Value != request.CustomerId) throw new CheckoutException("Cart does not belong to the requested customer.");
        if (cart.Items.Count == 0) throw new CheckoutException("Cart cannot be empty.");

        var address = await customerReader.GetAddressAsync(request.CustomerId, request.AddressId, cancellationToken)
    ?? throw new CheckoutException($"Address '{request.AddressId}' was not found.", CheckoutFailureType.NotFound);

        if (address.CustomerId != request.CustomerId) throw new CheckoutException("Address does not belong to the requested customer.");

        var skuIds = cart.Items.Select(item => item.SkuId).Distinct().ToArray();
        var catalogItems = await catalogReader.GetItemsAsync(skuIds, cancellationToken);

        if (catalogItems.Count != skuIds.Length) throw new CheckoutException("One or more cart items were not found in Catalog.");

        var catalogBySku = catalogItems.ToDictionary(item => item.SkuId);

        var lines = new List<OrderLine>();

        foreach (var cartItem in cart.Items)
        {
            if (!catalogBySku.TryGetValue(cartItem.SkuId, out var catalogItem))
                throw new CheckoutException($"SKU '{cartItem.SkuId}' was not found.");

            if (!catalogItem.IsActive)
                throw new CheckoutException($"SKU '{cartItem.SkuId}' is not available for checkout.");

            lines.Add(new OrderLine(catalogItem.SkuId, catalogItem.SkuCode, catalogItem.Description, catalogItem.UnitPrice, cartItem.Quantity, catalogItem.Currency));
        }

        var shippingAddress = new ShippingAddressSnapshot(address.Id, address.RecipientName, address.PostalCode, address.State, address.Municipality, address.Neighborhood, address.Street, address.ExteriorNumber, address.InteriorNumber, address.Reference);

        var order = new Order(Guid.NewGuid(), request.CustomerId, shippingAddress, lines, DateTimeOffset.UtcNow);

        await orderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CheckoutResult(order);
    }

    private static void ValidateRequest(CheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CartId == Guid.Empty) throw new ArgumentException("Cart id cannot be empty.", nameof(request));
        if (request.CustomerId == Guid.Empty) throw new ArgumentException("Customer id cannot be empty.", nameof(request));
        if (request.AddressId == Guid.Empty) throw new ArgumentException("Address id cannot be empty.", nameof(request));
    }
}