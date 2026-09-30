using CustomerEntity =
    YellowCola.Customer.Domain.Customers.Customer;

namespace YellowCola.Customer.Application.Customers;

public interface ICustomerRepository
{
    Task<CustomerEntity?> GetByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByExternalSubjectAsync(
        string externalSubject,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CustomerEntity customer,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task SaveDefaultAddressChangeAsync(Guid customerId,
        CancellationToken cancellationToken = default);
}