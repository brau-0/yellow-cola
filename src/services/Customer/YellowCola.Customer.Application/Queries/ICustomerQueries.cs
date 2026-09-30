namespace YellowCola.Customer.Application.Queries;

public interface ICustomerQueries
{
    Task<CustomerDto?> GetByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}