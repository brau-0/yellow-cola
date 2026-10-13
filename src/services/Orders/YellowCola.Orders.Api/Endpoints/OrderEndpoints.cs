using YellowCola.Orders.Api.Contracts;
using YellowCola.Orders.Application.Checkout;
using YellowCola.Orders.Application.Orders;

namespace YellowCola.Orders.Api.Endpoints;

internal static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders").WithTags("Orders");

        group.MapPost("/checkout", CheckoutAsync);
        group.MapGet("/{orderId:guid}", GetOrderAsync);

        return endpoints;
    }

    private static async Task<IResult> CheckoutAsync(CheckoutOrderRequest request, CheckoutApplicationService service, CancellationToken cancellationToken)
    {
        var result = await service.CheckoutAsync(new CheckoutRequest(request.CartId, request.CustomerId, request.AddressId), cancellationToken);

        return Results.Created($"/api/orders/{result.Order.Id}", result.Order.ToResponse());
    }

    private static async Task<IResult> GetOrderAsync(Guid orderId, IOrderRepository repository, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(orderId, cancellationToken);

        return order is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Order not found", detail: $"Order '{orderId}' was not found.")
            : Results.Ok(order.ToResponse());
    }
}