using HireFlow.Hiring.Application.Interfaces;
using HireFlow.Hiring.Infrastructure.Persistence;
using HireFlow.Hiring.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HireFlow.Hiring.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHiringInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HiringDatabase")
            ?? configuration["HIRING_DB_CONNECTION"]
            ?? "Host=localhost;Port=5432;Database=hireflow_hiring;Username=postgres;Password=12345678";

        services.AddDbContext<HiringDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(HiringDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        // Register application services
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
