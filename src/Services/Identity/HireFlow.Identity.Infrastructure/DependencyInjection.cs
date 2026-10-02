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

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        services.AddSingleton<HireFlow.Identity.Application.Interfaces.IPasswordHasher, Security.PasswordHasher>();
        services.AddSingleton<HireFlow.Identity.Application.Interfaces.ITokenService, Security.TokenService>();
        services.AddScoped<HireFlow.Identity.Application.Interfaces.IAuthService, Services.AuthService>();

        return services;
    }
}

