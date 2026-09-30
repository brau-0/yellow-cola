using YellowCola.Customer.Api.Endpoints;
using YellowCola.Customer.Application.Customers;
using YellowCola.Customer.Infrastructure;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<CustomerApplicationService>();

builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet(
    "/health",
    () =>
    {
        return Results.Ok(
            new
            {
                service = "customer",
                status = "healthy"
            });
    });
app.MapCustomerEndpoints();
app.Run();

public partial class Program;