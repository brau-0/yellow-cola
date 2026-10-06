using YellowCola.Cart.Domain.Carts;

namespace YellowCola.Cart.UnitTests;

public sealed class ShoppingCartTests
{
    [Fact]
    public void NewAnonymousCartShouldHaveNoCustomerAndNoItems()
    {
        var cart = new ShoppingCart(Guid.NewGuid());

        Assert.Null(cart.CustomerId);
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.TotalQuantity);
    }

    [Fact]
    public void AddItemShouldAddSkuToCart()
    {
        var cart = CreateCart();
        var skuId = Guid.NewGuid();

        cart.AddItem(skuId, 2);

        var item = Assert.Single(cart.Items);
        Assert.Equal(skuId, item.SkuId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(2, cart.TotalQuantity);
    }

    [Fact]
    public void AddingSameSkuShouldIncreaseExistingQuantity()
    {
        var cart = CreateCart();
        var skuId = Guid.NewGuid();

        cart.AddItem(skuId, 2);
        cart.AddItem(skuId, 3);

        var item = Assert.Single(cart.Items);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(5, cart.TotalQuantity);
    }

    [Fact]
    public void SetQuantityShouldReplaceExistingQuantity()
    {
        var cart = CreateCart();
        var skuId = Guid.NewGuid();

        cart.AddItem(skuId, 2);
        cart.SetQuantity(skuId, 7);

        Assert.Equal(7, Assert.Single(cart.Items).Quantity);
        Assert.Equal(7, cart.TotalQuantity);
    }

    [Fact]
    public void SetQuantityForMissingSkuShouldFail()
    {
        var cart = CreateCart();

        Action action = () => cart.SetQuantity(Guid.NewGuid(), 2);

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void RemoveItemShouldRemoveExistingSku()
    {
        var cart = CreateCart();
        var skuId = Guid.NewGuid();
        cart.AddItem(skuId, 2);

        var removed = cart.RemoveItem(skuId);

        Assert.True(removed);
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.TotalQuantity);
    }

    [Fact]
    public void RemovingMissingSkuShouldBeIdempotent()
    {
        var cart = CreateCart();

        var removed = cart.RemoveItem(Guid.NewGuid());

        Assert.False(removed);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AssignCustomerShouldConvertAnonymousCart()
    {
        var cart = CreateCart();
        var customerId = Guid.NewGuid();

        cart.AssignCustomer(customerId);

        Assert.Equal(customerId, cart.CustomerId);
    }

    [Fact]
    public void AssigningSameCustomerTwiceShouldBeIdempotent()
    {
        var customerId = Guid.NewGuid();
        var cart = new ShoppingCart(Guid.NewGuid(), customerId);

        cart.AssignCustomer(customerId);

        Assert.Equal(customerId, cart.CustomerId);
    }

    [Fact]
    public void AssigningDifferentCustomerShouldFail()
    {
        var cart = new ShoppingCart(Guid.NewGuid(), Guid.NewGuid());

        Action action = () => cart.AssignCustomer(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void QuantityMustBePositive()
    {
        var cart = CreateCart();

        Action action = () => cart.AddItem(Guid.NewGuid(), 0);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    private static ShoppingCart CreateCart() => new(Guid.NewGuid());
}