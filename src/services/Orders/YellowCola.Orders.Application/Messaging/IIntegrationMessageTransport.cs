namespace YellowCola.Orders.Application.Messaging;

public interface IIntegrationMessageTransport
{
    Task SendAsync(OutboundMessage message, CancellationToken cancellationToken = default);
}