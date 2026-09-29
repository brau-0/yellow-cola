using Microsoft.EntityFrameworkCore;

using YellowCola.Catalog.Domain.Categories;
using YellowCola.Catalog.Domain.Products;

namespace YellowCola.Catalog.Infrastructure.Persistence
{
    public sealed class CatalogDbContext(
    DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
    {
        public DbSet<Category> Categories => Set<Category>();

        public DbSet<Product> Products => Set<Product>();

        public DbSet<Sku> Skus => Set<Sku>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(CatalogDbContext).Assembly);
        }
    }
}
