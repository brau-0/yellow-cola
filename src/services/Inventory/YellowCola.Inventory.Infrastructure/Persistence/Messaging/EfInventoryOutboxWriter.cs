using Microsoft.EntityFrameworkCore;

using YellowCola.Checkout.Contracts.Messaging;
using YellowCola.Inventory.Application.Messaging;

namespace YellowCola.Inventory.Infrastructure.Persistence.Messaging;

internal sealed class EfInventoryOutboxWriter(InventoryDbContext dbContext) : IInventoryOutboxWriter
{

    public void Add(IntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        dbContext.OutboxMessages.Add(InventoryOutboxMessage.From(message));
    }
}