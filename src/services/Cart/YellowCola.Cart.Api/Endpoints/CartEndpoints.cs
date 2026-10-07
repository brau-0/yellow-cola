using YellowCola.Cart.Api.Contracts;
using YellowCola.Cart.Application.Carts;

namespace YellowCola.Cart.Api.Endpoints;

internal static class CartEndpoints
{
    public sealed record MergeCartRequest(Guid SourceCartId);
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/carts").WithTags("Cart");

        group.MapPost("/", CreateCartAsync);
        group.MapGet("/{cartId:guid}", GetCartAsync);
        group.MapPost("/{cartId:guid}/items", AddItemAsync);
        group.MapPut("/{cartId:guid}/items/{skuId:guid}", SetQuantityAsync);
        group.MapDelete("/{cartId:guid}/items/{skuId:guid}", RemoveItemAsync);
        group.MapPut("/{cartId:guid}/customer", AssignCustomerAsync);
        group.MapPost("/{cartId:guid}/merge", MergeCartAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateCartAsync(CreateCartRequest request, CartApplicationService service, CancellationToken cancellationToken)
    {
        var cart = await service.CreateAsync(request.CustomerId, cancellationToken);
        return Results.Created($"/api/carts/{cart.Id}", cart.ToResponse());
    }

    private static async Task<IResult> GetCartAsync(Guid cartId, CartApplicationService service, CancellationToken cancellationToken)
    {
        var cart = await service.GetAsync(cartId, cancellationToken);
        return cart is null ? NotFound(cartId) : Results.Ok(cart.ToResponse());
    }

    private static async Task<IResult> AddItemAsync(Guid cartId, AddCartItemRequest request, CartApplicationService service, CancellationToken cancellationToken)
    {
        var cart = await service.AddItemAsync(cartId, request.SkuId, request.Quantity, cancellationToken);
        return cart is null ? NotFound(cartId) : Results.Ok(cart.ToResponse());
    }

    private static async Task<IResult> SetQuantityAsync(Guid cartId, Guid skuId, SetCartItemQuantityRequest request, CartApplicationService service, CancellationToken cancellationToken)
    {
        var cart = await service.SetQuantityAsync(cartId, skuId, request.Quantity, cancellationToken);
        return cart is null ? NotFound(cartId) : Results.Ok(cart.ToResponse());
    }

    private static async Task<IResult> RemoveItemAsync(Guid cartId, Guid skuId, CartApplicationService service, CancellationToken cancellationToken)
    {
        var cart = await service.RemoveItemAsync(cartId, skuId, cancellationToken);
        return cart is null ? NotFound(cartId) : Results.Ok(cart.ToResponse());
    }

    private static async Task<IResult> AssignCustomerAsync(Guid cartId, AssignCartCustomerRequest request, CartApplicationService service, CancellationToken cancellationToken)
    {
        var cart = await service.AssignCustomerAsync(cartId, request.CustomerId, cancellationToken);
        return cart is null ? NotFound(cartId) : Results.Ok(cart.ToResponse());
    }
    private static async Task<IResult> MergeCartAsync(Guid cartId, MergeCartRequest request, CartApplicationService service, CancellationToken cancellationToken)
    {
        var cart = await service.MergeAnonymousCartAsync(cartId, request.SourceCartId, cancellationToken);

        return cart is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Cart not found", detail: "The target or source cart was not found.")
            : Results.Ok(cart.ToResponse());
    }
    private static IResult NotFound(Guid cartId) => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Cart not found", detail: $"Cart '{cartId}' was not found.");
}