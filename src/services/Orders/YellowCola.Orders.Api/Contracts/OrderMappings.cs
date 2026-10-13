using YellowCola.Orders.Domain.Orders;

namespace YellowCola.Orders.Api.Contracts;

internal static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order)
    {
        var address = new ShippingAddressResponse(
            order.ShippingAddress.SourceAddressId,
            order.ShippingAddress.RecipientName,
            order.ShippingAddress.PostalCode,
            order.ShippingAddress.State,
            order.ShippingAddress.Municipality,
            order.ShippingAddress.Neighborhood,
            order.ShippingAddress.Street,
            order.ShippingAddress.ExteriorNumber,
            order.ShippingAddress.InteriorNumber,
            order.ShippingAddress.Reference);

        var lines = order.Lines.Select(line => new OrderLineResponse(
            line.SkuId,
            line.SkuCode,
            line.Description,
            line.UnitPrice,
            line.Quantity,
            line.LineTotal,
            line.Currency)).ToArray();

        return new OrderResponse(order.Id, order.CustomerId, order.Status.ToString(), order.CreatedAtUtc, order.TotalAmount, order.Currency, address, lines);
    }
}