namespace YellowCola.Inventory.Application.Messaging;

public interface IInboxStore
{
    Task<bool> TryStartAsync(Guid messageId, string messageType, Guid correlationId, DateTimeOffset receivedAtUtc, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid messageId, DateTimeOffset processedAtUtc, CancellationToken cancellationToken = default);
}