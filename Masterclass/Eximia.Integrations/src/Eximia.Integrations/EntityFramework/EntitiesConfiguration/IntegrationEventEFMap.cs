using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Eximia.Integrations.IntegrationEvents;

namespace Eximia.Integrations.EntityFramework.EntitiesConfiguration;

public class IntegrationEventEfMap : IEntityTypeConfiguration<IntegrationEvent>
{
    public void Configure(EntityTypeBuilder<IntegrationEvent> builder)
    {
        builder.ToTable("integration_events");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnType("int8").HasColumnName("id").IsRequired();
        builder.Property(i => i.UniqueId).HasColumnType("uuid").HasColumnName("unique_id").IsRequired();

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? DateTime.SpecifyKind(v, DateTimeKind.Unspecified) : v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.OwnsOne(
            i => i.Entity,
            entity =>
            {
                entity.Property(e => e.Entity).HasColumnType("varchar(40)").HasColumnName("entity_type").IsRequired();
                entity.Property(e => e.Method).HasColumnType("varchar(10)").HasColumnName("method").IsRequired(false);
                entity.Property(e => e.DestinationQueue).HasColumnType("varchar(100)").HasColumnName("destination_queue").IsRequired(false);
                entity.Property(e => e.AggregateKey).HasColumnType("varchar(100)").HasColumnName("aggregate_key").IsRequired(false);
                entity.Property(e => e.CallbackRequired).HasColumnType("bool").HasColumnName("callback_required").IsRequired();
                entity.Property(e => e.Version).HasColumnType("bigint").HasColumnName("entity_version").IsRequired(false);

                entity.OwnsOne(
                    e => e.Origin,
                    origin =>
                    {
                        origin.Property(o => o.Name).HasColumnType("varchar(60)").HasColumnName("origin").IsRequired();
                        origin.Property(o => o.Key).HasColumnType("varchar(100)").HasColumnName("origin_key").IsRequired(false);
                    });

                entity.OwnsOne(
                    e => e.Destiny,
                    destiny =>
                    {
                        destiny.Property(d => d.Name).HasColumnType("varchar(60)").HasColumnName("destiny").IsRequired();
                        destiny.Property(d => d.Key).HasColumnType("varchar(100)").HasColumnName("destiny_key").IsRequired(false);
                    });
            });

        builder.OwnsOne(
            i => i.Control,
            control =>
            {
                control.Property(c => c.IntegrationStatus).HasConversion<int>().HasColumnType("int4").HasColumnName("status").IsRequired();
                control.Property(c => c.Mode).HasConversion<int>().HasColumnType("int4").HasColumnName("mode").IsRequired();
                control
                    .Property(c => c.CreatedAt)
                    .HasConversion(dateTimeConverter)
                    .HasColumnType("timestamp")
                    .HasColumnName("create_time")
                    .IsRequired();
                control
                    .Property(c => c.StartedAt)
                    .HasConversion(dateTimeConverter)
                    .HasColumnType("timestamp")
                    .HasColumnName("start_time")
                    .IsRequired(false);
                control
                    .Property(c => c.EndedAt)
                    .HasConversion(dateTimeConverter)
                    .HasColumnType("timestamp")
                    .HasColumnName("end_time")
                    .IsRequired(false);
                control.Property(c => c.IsCallback).HasColumnType("bool").HasColumnName("is_callback").IsRequired();
                control.Property(c => c.IsSync).HasColumnType("bool").HasColumnName("is_sync").IsRequired();
                control.Property(c => c.IsCallbackError).HasColumnType("bool").HasColumnName("is_callback_error").IsRequired();
            });

        builder.OwnsOne(
            i => i.Retry,
            retry =>
            {
                retry
                    .Property(r => r.LastTime)
                    .HasConversion(dateTimeConverter)
                    .HasColumnType("timestamp")
                    .HasColumnName("retry_last_time")
                    .IsRequired(false);
                retry.Property(r => r.Attempts).HasColumnType("int4").HasColumnName("retry_attempts").IsRequired();
                retry.Property(r => r.LastError).HasColumnType("varchar").HasColumnName("retry_last_error").IsRequired(false);
            });

        builder.Property(i => i.ControlCode).HasColumnType("varchar(60)").HasColumnName("control_code").IsRequired();
        builder.Property(i => i.IsPayloadLink).HasColumnType("bool").HasColumnName("is_payload_link").IsRequired();
        builder.Property(i => i.PayloadFile).HasColumnType("varchar(250)").HasColumnName("payload_file").IsRequired(false);
        builder.Property(i => i.PayloadFileId).HasColumnType("uuid").HasColumnName("file_id").IsRequired();
        builder.Property(i => i.SapQueryAction).HasColumnType("varchar(200)").HasColumnName("sap_query_action").IsRequired(false);
        builder.Property(i => i.StatusCode).HasColumnType("int4").HasColumnName("status_code").IsRequired(false);
    }
}