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
    [Fact]
    public void MergeAnonymousCartShouldCombineItems()
    {
        var skuA = Guid.NewGuid();
        var skuB = Guid.NewGuid();
        var skuC = Guid.NewGuid();

        var target = new ShoppingCart(Guid.NewGuid(), Guid.NewGuid());
        target.AddItem(skuA, 3);
        target.AddItem(skuC, 2);

        var source = new ShoppingCart(Guid.NewGuid());
        source.AddItem(skuA, 2);
        source.AddItem(skuB, 1);

        target.MergeAnonymousCart(source);

        Assert.Equal(3, target.Items.Count);
        Assert.Equal(5, target.Items.Single(item => item.SkuId == skuA).Quantity);
        Assert.Equal(1, target.Items.Single(item => item.SkuId == skuB).Quantity);
        Assert.Equal(2, target.Items.Single(item => item.SkuId == skuC).Quantity);
        Assert.Equal(8, target.TotalQuantity);
    }

    [Fact]
    public void MergeCustomerCartIntoAnotherCustomerCartShouldFail()
    {
        var target = new ShoppingCart(Guid.NewGuid(), Guid.NewGuid());
        var source = new ShoppingCart(Guid.NewGuid(), Guid.NewGuid());

        Action action = () => target.MergeAnonymousCart(source);

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void MergeCartWithItselfShouldFail()
    {
        var cart = new ShoppingCart(Guid.NewGuid(), Guid.NewGuid());

        Action action = () => cart.MergeAnonymousCart(cart);

        Assert.Throws<InvalidOperationException>(action);
    }

    private static ShoppingCart CreateCart() => new(Guid.NewGuid());
}