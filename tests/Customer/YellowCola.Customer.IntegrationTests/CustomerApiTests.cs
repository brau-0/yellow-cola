using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using YellowCola.Customer.Infrastructure.Persistence;
using YellowCola.Customer.IntegrationTests.Infrastructure;

namespace YellowCola.Customer.IntegrationTests;

public sealed class CustomerApiTests
    : IAsyncLifetime,
      IDisposable
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("yc_customer")
            .WithUsername("yellowcola")
            .WithPassword("yellowcola_test_only")
            .Build();

    private CustomerWebApplicationFactory? _factory;

    private HttpClient? _client;

    private HttpClient Client =>
        _client
        ?? throw new InvalidOperationException(
            "Test client has not been initialized.");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory =
            new CustomerWebApplicationFactory(
                _postgres.GetConnectionString());

        _client = _factory.CreateClient();

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<CustomerDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateCustomerShouldReturn201()
    {
        var response =
            await Client.PostAsJsonAsync(
                "/api/customers",
                new
                {
                    externalSubject =
                        "integration-subject-001"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<CreatedCustomerResponse>();

        Assert.NotNull(result);

        Assert.NotEqual(
            Guid.Empty,
            result.CustomerId);
    }

    [Fact]
    public async Task DuplicateExternalSubjectShouldReturn409()
    {
        const string externalSubject =
            "duplicate-subject";

        var firstResponse =
            await Client.PostAsJsonAsync(
                "/api/customers",
                new
                {
                    externalSubject
                });

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse =
            await Client.PostAsJsonAsync(
                "/api/customers",
                new
                {
                    externalSubject
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task FirstAddressShouldBecomeDefault()
    {
        var customerId =
            await CreateCustomerAsync(
                "first-address-subject");

        var addressId =
            await AddAddressAsync(
                customerId,
                "Home",
                "06600",
                "Ciudad de México",
                "Cuauhtémoc",
                "Juárez",
                "Paseo de la Reforma",
                "100");

        var customer =
            await GetCustomerAsync(
                customerId);

        Assert.Single(
            customer.Addresses);

        var address =
            customer.Addresses.Single();

        Assert.Equal(
            addressId,
            address.Id);

        Assert.True(
            address.IsDefault);
    }

    [Fact]
    public async Task SecondAddressShouldNotBecomeDefault()
    {
        var customerId =
            await CreateCustomerAsync(
                "second-address-subject");

        var firstAddressId =
            await AddAddressAsync(
                customerId,
                "Home",
                "06600",
                "Ciudad de México",
                "Cuauhtémoc",
                "Juárez",
                "Paseo de la Reforma",
                "100");

        var secondAddressId =
            await AddAddressAsync(
                customerId,
                "Office",
                "03100",
                "Ciudad de México",
                "Benito Juárez",
                "Del Valle",
                "Insurgentes Sur",
                "500");

        var customer =
            await GetCustomerAsync(
                customerId);

        Assert.Equal(
            2,
            customer.Addresses.Count);

        var firstAddress =
            customer.Addresses.Single(
                address =>
                    address.Id ==
                    firstAddressId);

        var secondAddress =
            customer.Addresses.Single(
                address =>
                    address.Id ==
                    secondAddressId);

        Assert.True(
            firstAddress.IsDefault);

        Assert.False(
            secondAddress.IsDefault);
    }

    [Fact]
    public async Task SetDefaultAddressShouldSwitchDefault()
    {
        var customerId =
            await CreateCustomerAsync(
                "switch-default-subject");

        var firstAddressId =
            await AddAddressAsync(
                customerId,
                "Home",
                "06600",
                "Ciudad de México",
                "Cuauhtémoc",
                "Juárez",
                "Paseo de la Reforma",
                "100");

        var secondAddressId =
            await AddAddressAsync(
                customerId,
                "Office",
                "03100",
                "Ciudad de México",
                "Benito Juárez",
                "Del Valle",
                "Insurgentes Sur",
                "500");

        var response =
            await Client.PutAsync(
                $"/api/customers/{customerId}" +
                $"/addresses/{secondAddressId}/default",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var customer =
            await GetCustomerAsync(
                customerId);

        Assert.Equal(
            2,
            customer.Addresses.Count);

        var first =
            customer.Addresses.Single(
                address =>
                    address.Id ==
                    firstAddressId);

        var second =
            customer.Addresses.Single(
                address =>
                    address.Id ==
                    secondAddressId);

        Assert.False(
            first.IsDefault);

        Assert.True(
            second.IsDefault);

        Assert.Single(
            customer.Addresses,
            address => address.IsDefault);
    }

    [Fact]
    public async Task UnknownCustomerShouldReturn404()
    {
        var response =
            await Client.GetAsync(
                $"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task InvalidPostalCodeShouldReturn400()
    {
        var customerId =
            await CreateCustomerAsync(
                "invalid-postal-code-subject");

        var response =
            await Client.PostAsJsonAsync(
                $"/api/customers/{customerId}/addresses",
                new
                {
                    label = "Home",
                    recipientName = "John Doe",
                    postalCode = "ABC12",
                    state = "Ciudad de México",
                    municipality = "Cuauhtémoc",
                    neighborhood = "Juárez",
                    street = "Paseo de la Reforma",
                    exteriorNumber = "100",
                    interiorNumber =
                        (string?)null,
                    reference =
                        (string?)null
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    private async Task<Guid> CreateCustomerAsync(
        string externalSubject)
    {
        var response =
            await Client.PostAsJsonAsync(
                "/api/customers",
                new
                {
                    externalSubject
                });

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<CreatedCustomerResponse>();

        Assert.NotNull(result);

        return result.CustomerId;
    }

    private async Task<Guid> AddAddressAsync(
        Guid customerId,
        string label,
        string postalCode,
        string state,
        string municipality,
        string neighborhood,
        string street,
        string exteriorNumber)
    {
        var response =
            await Client.PostAsJsonAsync(
                $"/api/customers/{customerId}/addresses",
                new
                {
                    label,
                    recipientName = "John Doe",
                    postalCode,
                    state,
                    municipality,
                    neighborhood,
                    street,
                    exteriorNumber,
                    interiorNumber =
                        (string?)null,
                    reference =
                        (string?)null
                });

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<CreatedAddressResponse>();

        Assert.NotNull(result);

        return result.AddressId;
    }

    private async Task<CustomerResponse>
        GetCustomerAsync(
            Guid customerId)
    {
        var response =
            await Client.GetAsync(
                $"/api/customers/{customerId}");

        response.EnsureSuccessStatusCode();

        var customer =
            await response.Content
                .ReadFromJsonAsync<CustomerResponse>();

        Assert.NotNull(customer);

        return customer;
    }

    private sealed class CreatedCustomerResponse
    {
        public Guid CustomerId { get; init; }
    }

    private sealed class CreatedAddressResponse
    {
        public Guid AddressId { get; init; }
    }

    private sealed class CustomerResponse
    {
        public Guid Id { get; init; }

        public string ExternalSubject { get; init; } =
            string.Empty;

        public List<AddressResponse> Addresses { get; init; } =
            [];
    }

    private sealed class AddressResponse
    {
        public Guid Id { get; init; }

        public string Label { get; init; } =
            string.Empty;

        public bool IsDefault { get; init; }
    }
}