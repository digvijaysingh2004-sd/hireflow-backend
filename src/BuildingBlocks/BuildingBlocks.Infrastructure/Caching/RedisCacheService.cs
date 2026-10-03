using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ICacheService _fallbackCache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer? redis,
        ICacheService fallbackCache,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _fallbackCache = fallbackCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            return await _fallbackCache.GetAsync<T>(key, cancellationToken);
        }

        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync($"cache:{key}");
            if (!value.HasValue) return default;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache GET failed for key {Key}. Falling back to memory cache.", key);
            return await _fallbackCache.GetAsync<T>(key, cancellationToken);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (value == null) return;

        if (_redis == null || !_redis.IsConnected)
        {
            await _fallbackCache.SetAsync(key, value, expiration, cancellationToken);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            var expiry = expiration ?? TimeSpan.FromMinutes(10);
            await db.StringSetAsync($"cache:{key}", json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache SET failed for key {Key}. Falling back to memory cache.", key);
            await _fallbackCache.SetAsync(key, value, expiration, cancellationToken);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            await _fallbackCache.RemoveAsync(key, cancellationToken);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync($"cache:{key}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache REMOVE failed for key {Key}. Falling back to memory cache.", key);
            await _fallbackCache.RemoveAsync(key, cancellationToken);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // Always remove from local fallback cache as well
        await _fallbackCache.RemoveByPrefixAsync(prefix, cancellationToken);

        if (_redis == null || !_redis.IsConnected) return;

        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                if (server.IsConnected)
                {
                    var keys = server.Keys(pattern: $"cache:{prefix}*").ToArray();
                    if (keys.Length > 0)
                    {
                        var db = _redis.GetDatabase();
                        await db.KeyDeleteAsync(keys);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache REMOVE BY PREFIX failed for prefix {Prefix}.", prefix);
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null) return cached;

        var value = await factory();
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }
}
