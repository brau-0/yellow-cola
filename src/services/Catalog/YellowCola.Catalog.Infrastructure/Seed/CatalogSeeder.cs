using YellowCola.Catalog.Domain.Categories;
using YellowCola.Catalog.Domain.Products;
using YellowCola.Catalog.Infrastructure.Persistence;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace YellowCola.Catalog.Infrastructure.Seed;

public sealed class CatalogSeeder(
    CatalogDbContext dbContext)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Products.AnyAsync(
                cancellationToken))
        {
            return;
        }

        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Seed",
            "catalog-v1.json");

        var json = await File.ReadAllTextAsync(
            path,
            cancellationToken);

        var seed = JsonSerializer.Deserialize<CatalogSeedFile>(
            json,
            SerializerOptions)
            ?? throw new InvalidOperationException(
                "Catalog seed file could not be deserialized.");

        if (seed.Categories is null)
        {
            throw new InvalidOperationException(
                "Catalog seed file does not contain a valid 'categories' collection.");
        }

        if (seed.Products is null)
        {
            throw new InvalidOperationException(
                "Catalog seed file does not contain a valid 'products' collection.");
        }
        var categories = new Dictionary<string, Category>(
            StringComparer.OrdinalIgnoreCase);

        // Level 1
        foreach (var item in seed.Categories
                     .Where(x => x.ParentSlug is null))
        {
            var category = new Category(
                Guid.NewGuid(),
                item.Name,
                item.Slug);

            categories[item.Slug] = category;

            dbContext.Categories.Add(category);
        }

        // Level 2
        foreach (var item in seed.Categories
                     .Where(x => x.ParentSlug is not null))
        {
            var parent = categories[item.ParentSlug!];

            var category = new Category(
                Guid.NewGuid(),
                item.Name,
                item.Slug,
                parent.Id);

            categories[item.Slug] = category;

            dbContext.Categories.Add(category);
        }

        foreach (var item in seed.Products)
        {
            var category =
                categories[item.CategoryLevel2];

            var product = new Product(
                Guid.NewGuid(),
                item.ProductId,
                category.Id,
                item.Brand,
                item.Name,
                item.Slug,
                item.ShortDescription,
                item.Description,
                item.PrimaryImageKey);

            foreach (var skuItem in item.Skus)
            {
                product.AddSku(
                    new Sku(
                        Guid.NewGuid(),
                        product.Id,
                        skuItem.Sku,
                        skuItem.Flavor,
                        skuItem.VolumeMl,
                        skuItem.ContainerType,
                        skuItem.UnitsPerPack,
                        skuItem.PackLabel,
                        skuItem.PriceMxn));
            }

            dbContext.Products.Add(product);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}