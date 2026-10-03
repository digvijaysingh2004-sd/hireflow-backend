using HireFlow.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HireFlow.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? configuration["IDENTITY_DB_CONNECTION"];
        var useInMemory = configuration["EF_PROVIDER"] == "InMemory" || string.IsNullOrEmpty(connectionString);


        services.AddDbContext<IdentityDbContext>(options =>
        {
            if (useInMemory)
            {
                options.UseInMemoryDatabase("hireflow_identity_test");
            }
            else
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                });
            }
        });


        services.AddSingleton<HireFlow.Identity.Application.Interfaces.IPasswordHasher, Security.PasswordHasher>();
        services.AddSingleton<HireFlow.Identity.Application.Interfaces.ITokenService, Security.TokenService>();
        services.AddHttpClient<HireFlow.Identity.Application.Interfaces.INotificationClient, Services.NotificationClient>();
        services.AddScoped<HireFlow.Identity.Application.Interfaces.IAuthService, Services.AuthService>();
        services.AddScoped<HireFlow.Identity.Application.Interfaces.IUserService, Services.UserService>();
        services.AddScoped<HireFlow.Identity.Application.Interfaces.IRoleService, Services.RoleService>();

        return services;
    }
}

