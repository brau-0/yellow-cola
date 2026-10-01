using Microsoft.EntityFrameworkCore;

using YellowCola.Inventory.Domain.InventoryItems;

namespace YellowCola.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems =>
        Set<InventoryItem>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InventoryDbContext).Assembly);
    }
}