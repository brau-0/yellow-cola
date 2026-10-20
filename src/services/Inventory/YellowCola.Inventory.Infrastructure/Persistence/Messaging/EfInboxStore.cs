using Microsoft.EntityFrameworkCore;

using YellowCola.Inventory.Application.Messaging;

namespace YellowCola.Inventory.Infrastructure.Persistence.Messaging;

internal sealed class EfInboxStore(InventoryDbContext dbContext) : IInboxStore
{
    public async Task<bool> TryStartAsync(Guid messageId, string messageType, Guid correlationId, DateTimeOffset receivedAtUtc, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty) throw new ArgumentException("Message id cannot be empty.", nameof(messageId));
        if (correlationId == Guid.Empty) throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        if (string.IsNullOrWhiteSpace(messageType)) throw new ArgumentException("Message type is required.", nameof(messageType));
        if (receivedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("ReceivedAtUtc must be UTC.", nameof(receivedAtUtc));

        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO inbox_messages (message_id, message_type, correlation_id, received_at_utc, processed_at_utc)
            VALUES ({messageId}, {messageType}, {correlationId}, {receivedAtUtc}, NULL)
            ON CONFLICT (message_id) DO NOTHING
            """, cancellationToken);

        return affected == 1;
    }

    public async Task MarkProcessedAsync(Guid messageId, DateTimeOffset processedAtUtc, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty) throw new ArgumentException("Message id cannot be empty.", nameof(messageId));
        if (processedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("ProcessedAtUtc must be UTC.", nameof(processedAtUtc));

        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE inbox_messages
            SET processed_at_utc = {processedAtUtc}
            WHERE message_id = {messageId}
              AND processed_at_utc IS NULL
            """, cancellationToken);

        if (affected != 1) throw new InvalidOperationException($"Inbox message '{messageId}' could not be marked as processed.");
    }
}