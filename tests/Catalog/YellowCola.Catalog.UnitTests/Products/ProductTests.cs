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
            Guid.NewGuid(),
            "Yellow Cola",
            "Yellow Cola Original",
            "yellow-cola-original",
            "Classic Yellow Cola.");

        var sku = new Sku(
            Guid.NewGuid(),
            productId,
            "YC-ORI-355-CAN-06",
            355,
            "CAN",
            6,
            89.90m);

        product.AddSku(sku);

        Assert.Single(product.Skus);
    }

    [Fact]
    public void AddSkuShouldRejectDuplicateSkuCode()
    {
        var productId = Guid.NewGuid();

        var product = new Product(
            productId,
            Guid.NewGuid(),
            "Yellow Cola",
            "Yellow Cola Original",
            "yellow-cola-original",
            "Classic Yellow Cola.");

        product.AddSku(
            new Sku(
                Guid.NewGuid(),
                productId,
                "YC-ORI-355-CAN-06",
                355,
                "CAN",
                6,
                89.90m));

        var duplicatedSku = new Sku(
            Guid.NewGuid(),
            productId,
            "yc-ori-355-can-06",
            355,
            "CAN",
            6,
            89.90m);

        var exception = Assert.Throws<InvalidOperationException>(
            () => product.AddSku(duplicatedSku));

        Assert.Contains(
            "already exists",
            exception.Message);
    }

    [Fact]
    public void ChangePriceShouldRejectNegativePrice()
    {
        var sku = new Sku(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "YC-ORI-355-CAN-06",
            355,
            "CAN",
            6,
            89.90m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => sku.ChangePrice(-1m));
    }
}
