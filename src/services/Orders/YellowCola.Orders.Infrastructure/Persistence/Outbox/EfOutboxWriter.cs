using YellowCola.Checkout.Contracts.Messaging;
using YellowCola.Orders.Application.Messaging;

namespace YellowCola.Orders.Infrastructure.Persistence.Outbox;

internal sealed class EfOutboxWriter(OrdersDbContext dbContext) : IOutboxWriter
{
    public void Add(IntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        dbContext.OutboxMessages.Add(OutboxMessage.From(message));
    }
}