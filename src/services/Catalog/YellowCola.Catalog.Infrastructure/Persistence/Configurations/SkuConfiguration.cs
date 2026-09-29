using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using YellowCola.Catalog.Domain.Products;

namespace YellowCola.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class SkuConfiguration
    : IEntityTypeConfiguration<Sku>
{
    public void Configure(
        EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("skus");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.UnitSizeMl)
            .HasColumnName("unit_size_ml")
            .IsRequired();

        builder.Property(x => x.PackageType)
            .HasColumnName("package_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.UnitsPerPack)
            .HasColumnName("units_per_pack")
            .IsRequired();

        builder.Property(x => x.Price)
            .HasColumnName("price")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.Flavor)
            .HasColumnName("flavor")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PackLabel)
            .HasColumnName("pack_label")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();
    }
}