using CustomerEntity =
    YellowCola.Customer.Domain.Customers.Customer;

namespace YellowCola.Customer.Application.Customers;

public sealed class CustomerApplicationService(
    ICustomerRepository repository)
{
    public async Task<Guid?> CreateCustomerAsync(
        string externalSubject,
        CancellationToken cancellationToken = default)
    {
        var normalizedSubject =
            externalSubject.Trim();

        var alreadyExists =
            await repository.ExistsByExternalSubjectAsync(
                normalizedSubject,
                cancellationToken);

        if (alreadyExists)
        {
            return null;
        }

        var customer =
            new CustomerEntity(
                Guid.NewGuid(),
                normalizedSubject);

        await repository.AddAsync(
            customer,
            cancellationToken);

        await repository.SaveChangesAsync(
            cancellationToken);

        return customer.Id;
    }

    public async Task<Guid?> AddAddressAsync(
        Guid customerId,
        AddAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        var customer =
            await repository.GetByIdAsync(
                customerId,
                cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var address =
            customer.AddAddress(
                Guid.NewGuid(),
                command.Label,
                command.RecipientName,
                command.PostalCode,
                command.State,
                command.Municipality,
                command.Neighborhood,
                command.Street,
                command.ExteriorNumber,
                command.InteriorNumber,
                command.Reference);

        await repository.SaveChangesAsync(
            cancellationToken);

        return address.Id;
    }

    public async Task<bool> SetDefaultAddressAsync(
    Guid customerId,
    Guid addressId,
    CancellationToken cancellationToken = default)
    {
        var customer =
            await repository.GetByIdAsync(
                customerId,
                cancellationToken);

        if (customer is null)
        {
            return false;
        }

        var selectedAddress =
            customer.Addresses.SingleOrDefault(
                address => address.Id == addressId);

        if (selectedAddress is null)
        {
            return false;
        }
   
        if (selectedAddress.IsDefault)
        {
            return true;
        }

        customer.SetDefaultAddress(addressId);

        await repository.SaveDefaultAddressChangeAsync(
            customerId,
            cancellationToken);

        return true;
    }
}