namespace YellowCola.Catalog.Application.Queries;

public sealed record ProductSearchRequest(
    int Page = 1,
    int PageSize = 20,
    string? Brand = null,
    string? Category = null);
