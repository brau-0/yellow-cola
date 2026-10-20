using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using YellowCola.Inventory.Infrastructure.Persistence.Messaging;

namespace YellowCola.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class InventoryOutboxMessageConfiguration : IEntityTypeConfiguration<InventoryOutboxMessage>
{
    public void Configure(EntityTypeBuilder<InventoryOutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", table =>
        {
            table.HasCheckConstraint("CK_outbox_messages_schema_version_positive", "\"schema_version\" > 0");
            table.HasCheckConstraint("CK_outbox_messages_publish_attempts_non_negative", "\"publish_attempts\" >= 0");
        });

        builder.HasKey(message => message.MessageId);

        builder.Property(message => message.MessageId).HasColumnName("message_id").ValueGeneratedNever();
        builder.Property(message => message.MessageType).HasColumnName("message_type").HasMaxLength(200).IsRequired();
        builder.Property(message => message.SchemaVersion).HasColumnName("schema_version").IsRequired();
        builder.Property(message => message.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(message => message.CausationId).HasColumnName("causation_id");
        builder.Property(message => message.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(message => message.PublishedAtUtc).HasColumnName("published_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(message => message.PublishAttempts).HasColumnName("publish_attempts").HasDefaultValue(0).IsRequired();
        builder.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(2000);

        builder.HasIndex(message => new { message.PublishedAtUtc, message.CreatedAtUtc }).HasDatabaseName("IX_outbox_messages_published_created");
        builder.HasIndex(message => message.CorrelationId).HasDatabaseName("IX_outbox_messages_correlation_id");
    }
}