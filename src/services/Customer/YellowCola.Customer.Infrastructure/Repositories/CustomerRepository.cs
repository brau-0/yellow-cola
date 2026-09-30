using Microsoft.EntityFrameworkCore;

using YellowCola.Customer.Application.Customers;
using YellowCola.Customer.Infrastructure.Persistence;

using CustomerEntity =
    YellowCola.Customer.Domain.Customers.Customer;

namespace YellowCola.Customer.Infrastructure.Repositories;

internal sealed class CustomerRepository(
    CustomerDbContext dbContext)
    : ICustomerRepository
{
    public Task<CustomerEntity?> GetByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Customers
            .Include(customer => customer.Addresses)
            .SingleOrDefaultAsync(
                customer => customer.Id == customerId,
                cancellationToken);
    }

    public Task<bool> ExistsByExternalSubjectAsync(
        string externalSubject,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Customers.AnyAsync(
            customer =>
                customer.ExternalSubject ==
                externalSubject,
            cancellationToken);
    }

    public async Task AddAsync(
        CustomerEntity customer,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Customers.AddAsync(
            customer,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
    CancellationToken cancellationToken = default)
    {       
       
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
    public async Task SaveDefaultAddressChangeAsync(
    Guid customerId,
    CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            // Paso 1:
            // eliminamos primero el default actual
            // directamente en PostgreSQL.
            await dbContext.Addresses
                .Where(address =>
                    address.CustomerId == customerId &&
                    address.IsDefault)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            address => address.IsDefault,
                            false),
                    cancellationToken);

            // Paso 2:
            // el Change Tracker ya contiene:
            //
            // old default -> false
            // new default -> true
            //
            // Ahora puede persistirlo sin colisionar
            // contra el índice único.
            await dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }
}