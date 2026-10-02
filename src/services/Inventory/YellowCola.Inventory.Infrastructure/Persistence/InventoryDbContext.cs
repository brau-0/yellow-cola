using Microsoft.EntityFrameworkCore;

using YellowCola.Inventory.Domain.InventoryItems;
using YellowCola.Inventory.Domain.Reservations;

namespace YellowCola.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems =>
        Set<InventoryItem>();

    public DbSet<InventoryReservation>
        InventoryReservations =>
            Set<InventoryReservation>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InventoryDbContext).Assembly);
    }
}