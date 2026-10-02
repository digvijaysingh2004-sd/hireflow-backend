using HireFlow.Notification.Application.Interfaces;
using HireFlow.Notification.Infrastructure.Persistence;
using HireFlow.Notification.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HireFlow.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationDatabase")
            ?? configuration["NOTIFICATION_DB_CONNECTION"]
            ?? "Host=localhost;Port=5432;Database=hireflow_notification;Username=postgres;Password=12345678";

        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(NotificationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        // Register SMTP / Email delivery
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Register application services
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IOutboxService, OutboxService>();

        return services;
    }
}
