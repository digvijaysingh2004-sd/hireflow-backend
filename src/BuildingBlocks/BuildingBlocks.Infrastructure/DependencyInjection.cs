using BuildingBlocks.Infrastructure.Caching;
using BuildingBlocks.Infrastructure.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Register Microsoft MemoryCache
        services.AddMemoryCache();

        // 2. Always register Memory providers as singleton base/fallback
        services.AddSingleton<MemoryRateLimiter>();
        services.AddSingleton<MemoryCacheService>();

        // 3. Check for Redis configuration (Upstash / Render / local)
        var redisConnectionString = configuration["Redis:ConnectionString"]
            ?? configuration["Redis__ConnectionString"]
            ?? configuration["REDIS_URL"];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("BuildingBlocks.Redis");
                try
                {
                    var options = ConfigurationOptions.Parse(redisConnectionString);
                    options.AbortOnConnectFail = false; // Never crash app on startup if Redis is temporarily unreachable
                    options.ConnectTimeout = 5000;
                    options.SyncTimeout = 5000;
                    options.Ssl = redisConnectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase) || redisConnectionString.Contains("ssl=True", StringComparison.OrdinalIgnoreCase);
                    return ConnectionMultiplexer.Connect(options);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to connect to Redis at startup. Falling back to in-memory providers.");
                    var dummyOptions = new ConfigurationOptions { AbortOnConnectFail = false };
                    return ConnectionMultiplexer.Connect(dummyOptions);
                }
            });

            services.AddSingleton<IRateLimiter>(sp =>
            {
                var redis = sp.GetService<IConnectionMultiplexer>();
                var memoryFallback = sp.GetRequiredService<MemoryRateLimiter>();
                var logger = sp.GetRequiredService<ILogger<RedisRateLimiter>>();
                return new RedisRateLimiter(redis, memoryFallback, logger);
            });

            services.AddSingleton<ICacheService>(sp =>
            {
                var redis = sp.GetService<IConnectionMultiplexer>();
                var memoryFallback = sp.GetRequiredService<MemoryCacheService>();
                var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
                return new RedisCacheService(redis, memoryFallback, logger);
            });
        }
        else
        {
            // Zero-download local development mode: in-memory limiter & cache
            services.AddSingleton<IRateLimiter>(sp => sp.GetRequiredService<MemoryRateLimiter>());
            services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<MemoryCacheService>());
        }

        return services;
    }
}
