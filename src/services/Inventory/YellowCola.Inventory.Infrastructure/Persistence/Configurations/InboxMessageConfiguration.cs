using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using YellowCola.Inventory.Infrastructure.Persistence.Messaging;

namespace YellowCola.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(message => message.MessageId);

        builder.Property(message => message.MessageId).HasColumnName("message_id").ValueGeneratedNever();
        builder.Property(message => message.MessageType).HasColumnName("message_type").HasMaxLength(200).IsRequired();
        builder.Property(message => message.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(message => message.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(message => message.ProcessedAtUtc).HasColumnName("processed_at_utc").HasColumnType("timestamp with time zone");

        builder.HasIndex(message => new { message.ProcessedAtUtc, message.ReceivedAtUtc }).HasDatabaseName("IX_inbox_messages_processed_received");
        builder.HasIndex(message => message.CorrelationId).HasDatabaseName("IX_inbox_messages_correlation_id");
    }
}