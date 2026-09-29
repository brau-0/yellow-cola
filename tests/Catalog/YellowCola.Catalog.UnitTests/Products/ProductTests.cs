using System;
using System.Collections.Generic;
using System.Text;

using YellowCola.Catalog.Domain.Products;

namespace YellowCola.Catalog.UnitTests.Products;

public sealed class ProductTests
{
    [Fact]
    public void AddSkuShouldAddSkuWhenSkuIsValid()
    {
        var productId = Guid.NewGuid();

        var product = new Product(
            productId,
            "YC-P001",
            Guid.NewGuid(),
            "Yellow Cola",
            "Yellow Cola Original",
            "yellow-cola-original",
            "Cola clásica.",
            "Cola clásica con perfil caramelizado.",
            "yc-p001-main");

        var sku = new Sku(
            Guid.NewGuid(),
            productId,
            "YC-ORI-355-CAN-06",
            "ORI",
            355,
            "CAN",
            6,
            "6 x 355 ml",
            72m);

        product.AddSku(sku);

        Assert.Single(product.Skus);
    }

    [Fact]
    public void AddSkuShouldRejectDuplicateSkuCode()
    {
        var productId = Guid.NewGuid();

        var product = new Product(
             productId,
             "YC-P001",
             Guid.NewGuid(),
             "Yellow Cola",
             "Yellow Cola Original",
             "yellow-cola-original",
             "Cola clásica.",
             "Cola clásica con perfil caramelizado.",
             "yc-p001-main");

        product.AddSku(
            new Sku(
                Guid.NewGuid(),
                productId,
                "YC-ORI-355-CAN-06",
                "ORI",
                355,
                "CAN",
                6,
                "6 x 355 ml",
                72m));

        var duplicatedSku = new Sku(
            Guid.NewGuid(),
            productId,
            "YC-ORI-355-CAN-06",
            "ORI",
            355,
            "CAN",
            6,
            "6 x 355 ml",
            72m);

        var exception = Assert.Throws<InvalidOperationException>(
            () => product.AddSku(duplicatedSku));

        Assert.Contains(
            "already exists",
            exception.Message);
    }

    [Fact]
    public void ChangePriceShouldRejectNegativePrice()
    {
        var productId = Guid.NewGuid();
        var sku = new Sku(
            Guid.NewGuid(),
            productId,
            "YC-ORI-355-CAN-06",
            "ORI",
            355,
            "CAN",
            6,
            "6 x 355 ml",
            72m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => sku.ChangePrice(-1m));
    }
}
