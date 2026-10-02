using HireFlow.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireFlow.Notification.Infrastructure.Persistence.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TemplateKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.TemplateKey)
            .IsUnique();

        builder.Property(t => t.SubjectTemplate)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.BodyTemplate)
            .IsRequired();

        builder.Property(t => t.Locale)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("en-US");

        builder.Property(t => t.Version)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.Property(t => t.UpdatedAtUtc)
            .IsRequired();
    }
}

public class EmailNotificationConfiguration : IEntityTypeConfiguration<EmailNotification>
{
    public void Configure(EntityTypeBuilder<EmailNotification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.TemplateKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.RecipientEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(n => n.RecipientEmailMasked)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(n => n.Subject)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(n => n.Body)
            .IsRequired();

        builder.Property(n => n.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Queued");

        builder.Property(n => n.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(n => n.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(n => n.IdempotencyKey)
            .HasMaxLength(255);

        builder.HasIndex(n => n.IdempotencyKey);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.CreatedAtUtc);
    }
}

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.PayloadJson)
            .IsRequired();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Pending");

        builder.Property(o => o.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(o => o.Status);
    }
}

public class NotificationAuditLogConfiguration : IEntityTypeConfiguration<NotificationAuditLog>
{
    public void Configure(EntityTypeBuilder<NotificationAuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .HasMaxLength(100);

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();
    }
}
