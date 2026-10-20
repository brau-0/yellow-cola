using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;

using YellowCola.Orders.Application.Messaging;

namespace YellowCola.Orders.Infrastructure.Persistence.Outbox;

internal sealed class OutboxPublisherProcessor(
    OrdersDbContext dbContext,
    IIntegrationMessageTransport transport)
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Each outbox message must record transport failures without stopping the remaining publisher loop.")]
    public async Task<int> PublishPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var messages = await dbContext.OutboxMessages
            .Where(message => message.PublishedAtUtc == null)
            .OrderBy(message => message.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await transport.SendAsync(message.ToOutboundMessage(), cancellationToken);
                message.MarkPublished(DateTimeOffset.UtcNow);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.RecordFailure(exception.Message);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }
}