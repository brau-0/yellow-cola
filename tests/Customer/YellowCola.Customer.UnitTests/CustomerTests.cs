using CustomerEntity =
    YellowCola.Customer.Domain.Customers.Customer;
namespace YellowCola.Customer.UnitTests;

public sealed class CustomerTests
{
    [Fact]
    public void FirstAddressShouldBecomeDefault()
    {
        var customer =
            CreateCustomer();

        var address =
            customer.AddAddress(
                Guid.NewGuid(),
                "Home",
                "John Doe",
                "06600",
                "Ciudad de México",
                "Cuauhtémoc",
                "Juárez",
                "Paseo de la Reforma",
                "100",
                null,
                null);

        Assert.True(address.IsDefault);
    }

    [Fact]
    public void SecondAddressShouldNotBecomeDefault()
    {
        var customer =
            CreateCustomer();

        customer.AddAddress(
            Guid.NewGuid(),
            "Home",
            "John Doe",
            "06600",
            "Ciudad de México",
            "Cuauhtémoc",
            "Juárez",
            "Paseo de la Reforma",
            "100",
            null,
            null);

        var second =
            customer.AddAddress(
                Guid.NewGuid(),
                "Office",
                "John Doe",
                "03100",
                "Ciudad de México",
                "Benito Juárez",
                "Del Valle",
                "Insurgentes Sur",
                "500",
                null,
                null);

        Assert.False(second.IsDefault);
    }

    [Fact]
    public void SetDefaultAddressShouldChangeDefault()
    {
        var customer =
            CreateCustomer();

        var first =
            customer.AddAddress(
                Guid.NewGuid(),
                "Home",
                "John Doe",
                "06600",
                "Ciudad de México",
                "Cuauhtémoc",
                "Juárez",
                "Paseo de la Reforma",
                "100",
                null,
                null);

        var second =
            customer.AddAddress(
                Guid.NewGuid(),
                "Office",
                "John Doe",
                "03100",
                "Ciudad de México",
                "Benito Juárez",
                "Del Valle",
                "Insurgentes Sur",
                "500",
                null,
                null);

        customer.SetDefaultAddress(second.Id);

        Assert.False(first.IsDefault);
        Assert.True(second.IsDefault);
    }

    [Fact]
    public void SetDefaultAddressWithUnknownAddressShouldFail()
    {
        var customer =
            CreateCustomer();



        Action action = () =>
            customer.SetDefaultAddress(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void AddAddressWithInvalidPostalCodeShouldFail()
    {
        var customer =
            CreateCustomer();

        Action action = () =>
            customer.AddAddress(
                Guid.NewGuid(),
                "Home",
                "John Doe",
                "ABC12",
                "Ciudad de México",
                "Cuauhtémoc",
                "Juárez",
                "Paseo de la Reforma",
                "100",
                null,
                null);

        Assert.Throws<ArgumentException>(
            action);
    }
    private static CustomerEntity CreateCustomer()
    {
        return new CustomerEntity(
            Guid.NewGuid(),
            "test-subject-123");
    }
}