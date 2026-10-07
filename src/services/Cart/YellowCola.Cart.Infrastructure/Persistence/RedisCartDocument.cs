namespace YellowCola.Cart.Infrastructure.Persistence;

internal sealed record RedisCartDocument(Guid Id, Guid? CustomerId, List<RedisCartItemDocument> Items);

internal sealed record RedisCartItemDocument(Guid SkuId, int Quantity);