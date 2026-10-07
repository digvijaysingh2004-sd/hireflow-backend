using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HireFlow.Notification.Infrastructure.Persistence;

public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=hireflow_notification;Username=postgres;Password=2003");

        return new NotificationDbContext(optionsBuilder.Options);
    }
}
