using HireFlow.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HireFlow.Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmailTemplate> Templates => Set<EmailTemplate>();
    public DbSet<EmailNotification> Notifications => Set<EmailNotification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<NotificationAuditLog> AuditLogs => Set<NotificationAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
