namespace YellowCola.Cart.Api.Contracts;

public sealed record CreateCartRequest(Guid? CustomerId);
public sealed record AddCartItemRequest(Guid SkuId, int Quantity);
public sealed record SetCartItemQuantityRequest(int Quantity);
public sealed record AssignCartCustomerRequest(Guid CustomerId);
public sealed record CartItemResponse(Guid SkuId, int Quantity);
public sealed record CartResponse(Guid Id, Guid? CustomerId, IReadOnlyCollection<CartItemResponse> Items, int TotalQuantity);
public sealed record MergeCartRequest(Guid SourceCartId);