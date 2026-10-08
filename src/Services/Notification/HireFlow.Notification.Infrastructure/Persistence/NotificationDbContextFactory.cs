using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HireFlow.Notification.Infrastructure.Persistence;

public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NOTIFICATION_DB_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__NotificationDatabase")
            ?? "Host=localhost;Port=5432;Database=hireflow_notification;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new NotificationDbContext(optionsBuilder.Options);
    }
}
