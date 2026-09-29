using System.Net;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using YellowCola.Catalog.Infrastructure.Persistence;
using YellowCola.Catalog.Infrastructure.Seed;
using YellowCola.Catalog.IntegrationTests.Infrastructure;

namespace YellowCola.Catalog.IntegrationTests;

public sealed class CatalogApiTests : IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("yc_catalog")
            .WithUsername("yellowcola")
            .WithPassword("yellowcola_test_only")
            .Build();

    private CatalogWebApplicationFactory? _factory;

    private HttpClient? _client;

    private HttpClient Client =>
        _client
        ?? throw new InvalidOperationException(
            "Test client has not been initialized.");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory =
            new CatalogWebApplicationFactory(
                _postgres.GetConnectionString());

        _client = _factory.CreateClient();

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<CatalogDbContext>();

        await dbContext.Database.MigrateAsync();

        var seeder =
            scope.ServiceProvider
                .GetRequiredService<CatalogSeeder>();

        await seeder.SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CategoriesShouldReturn12Categories()
    {
        var response = await Client.GetAsync(
            "/api/catalog/categories");

        response.EnsureSuccessStatusCode();

        var json = await response.Content
            .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        Assert.Equal(
            12,
            document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task ProductsShouldReturn20Products()
    {
        var response = await Client.GetAsync(
            "/api/catalog/products");

        response.EnsureSuccessStatusCode();

        var json = await response.Content
            .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.Equal(
            20,
            root.GetProperty("totalCount").GetInt32());

        Assert.Equal(
            20,
            root.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task ProductsShouldFilterByBrand()
    {
        var response = await Client.GetAsync(
            "/api/catalog/products?brand=Yellow%20Cola");

        response.EnsureSuccessStatusCode();

        var json = await response.Content
            .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        Assert.Equal(
            4,
            document.RootElement
                .GetProperty("totalCount")
                .GetInt32());
    }

    [Fact]
    public async Task ProductDetailShouldReturnProductAndSkus()
    {
        var response = await Client.GetAsync(
            "/api/catalog/products/yellow-cola-original");

        response.EnsureSuccessStatusCode();

        var json = await response.Content
            .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.Equal(
            "YC-P001",
            root.GetProperty("code").GetString());

        Assert.Equal(
            "Yellow Cola Original",
            root.GetProperty("name").GetString());

        Assert.True(
            root.GetProperty("skus").GetArrayLength() > 0);
    }

    [Fact]
    public async Task UnknownProductShouldReturn404()
    {
        var response = await Client.GetAsync(
            "/api/catalog/products/product-that-does-not-exist");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}
