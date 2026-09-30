using YellowCola.Customer.Api.Contracts;
using YellowCola.Customer.Application.Customers;
using YellowCola.Customer.Application.Queries;

namespace YellowCola.Customer.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder
        MapCustomerEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints.MapGroup(
                "/api/customers");

        group.MapPost(
            "/",
            CreateCustomerAsync);

        group.MapGet(
            "/{customerId:guid}",
            GetCustomerAsync);

        group.MapPost(
            "/{customerId:guid}/addresses",
            AddAddressAsync);

        group.MapPut(
            "/{customerId:guid}/addresses/{addressId:guid}/default",
            SetDefaultAddressAsync);

        return endpoints;
    }

    private static async Task<IResult>
        CreateCustomerAsync(
            CreateCustomerRequest request,
            CustomerApplicationService service,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                request.ExternalSubject))
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "ExternalSubject is required."
                });
        }

        var customerId =
            await service.CreateCustomerAsync(
                request.ExternalSubject,
                cancellationToken);

        if (customerId is null)
        {
            return Results.Conflict(
                new
                {
                    error =
                        "A customer with this external subject already exists."
                });
        }

        return Results.Created(
            $"/api/customers/{customerId}",
            new
            {
                customerId
            });
    }

    private static async Task<IResult>
        GetCustomerAsync(
            Guid customerId,
            ICustomerQueries queries,
            CancellationToken cancellationToken)
    {
        var customer =
            await queries.GetByIdAsync(
                customerId,
                cancellationToken);

        return customer is null
            ? Results.NotFound()
            : Results.Ok(customer);
    }

    private static async Task<IResult>
        AddAddressAsync(
            Guid customerId,
            CreateAddressRequest request,
            CustomerApplicationService service,
            CancellationToken cancellationToken)
    {
        var command =
            new AddAddressCommand(
                request.Label,
                request.RecipientName,
                request.PostalCode,
                request.State,
                request.Municipality,
                request.Neighborhood,
                request.Street,
                request.ExteriorNumber,
                request.InteriorNumber,
                request.Reference);

        var addressId =
            await service.AddAddressAsync(
                customerId,
                command,
                cancellationToken);

        if (addressId is null)
        {
            return Results.NotFound();
        }

        return Results.Created(
            $"/api/customers/{customerId}",
            new
            {
                addressId
            });
    }

    private static async Task<IResult>
        SetDefaultAddressAsync(
            Guid customerId,
            Guid addressId,
            CustomerApplicationService service,
            CancellationToken cancellationToken)
    {
        var updated =
            await service.SetDefaultAddressAsync(
                customerId,
                addressId,
                cancellationToken);

        return updated
            ? Results.NoContent()
            : Results.NotFound();
    }
}