using Microsoft.EntityFrameworkCore;

using YellowCola.Customer.Application.Queries;
using YellowCola.Customer.Infrastructure.Persistence;

namespace YellowCola.Customer.Infrastructure.Queries;

internal sealed class CustomerQueries(
    CustomerDbContext dbContext)
    : ICustomerQueries
{
    public Task<CustomerDto?> GetByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Customers
            .AsNoTracking()
            .Where(customer =>
                customer.Id == customerId)
            .Select(customer =>
                new CustomerDto(
                    customer.Id,
                    customer.ExternalSubject,

                    customer.Addresses
                        .OrderByDescending(
                            address =>
                                address.IsDefault)
                        .ThenBy(
                            address =>
                                address.Label)
                        .Select(address =>
                            new AddressDto(
                                address.Id,
                                address.Label,
                                address.RecipientName,
                                address.PostalCode,
                                address.State,
                                address.Municipality,
                                address.Neighborhood,
                                address.Street,
                                address.ExteriorNumber,
                                address.InteriorNumber,
                                address.Reference,
                                address.IsDefault))
                        .ToList()))
            .SingleOrDefaultAsync(
                cancellationToken);
    }
}