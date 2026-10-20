using YellowCola.Checkout.Contracts.Messaging;

namespace YellowCola.Orders.Application.Messaging;

public interface IOutboxWriter
{
    void Add(IntegrationMessage message);
}