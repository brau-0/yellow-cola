using YellowCola.Checkout.Contracts.Messaging;

namespace YellowCola.Inventory.Application.Messaging;

public interface IInventoryOutboxWriter
{
    void Add(IntegrationMessage message);
}