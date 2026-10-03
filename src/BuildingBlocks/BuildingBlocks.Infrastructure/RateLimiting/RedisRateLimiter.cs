using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.RateLimiting;

public sealed class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IRateLimiter _fallbackLimiter;
    private readonly ILogger<RedisRateLimiter> _logger;

    // Lua script for atomic sliding window rate limiting via Redis Sorted Set
    private const string SlidingWindowLuaScript = @"
        local key = KEYS[1]
        local now = tonumber(ARGV[1])
        local windowSeconds = tonumber(ARGV[2])
        local maxRequests = tonumber(ARGV[3])
        local clearBefore = now - windowSeconds

        -- Remove old timestamps outside current sliding window
        redis.call('ZREMRANGEBYSCORE', key, 0, clearBefore)

        -- Count current entries in window
        local currentCount = redis.call('ZCARD', key)

        if currentCount < maxRequests then
            -- Add current timestamp as score and unique member
            redis.call('ZADD', key, now, now .. '-' .. redis.call('INCR', key .. ':seq'))
            redis.call('EXPIRE', key, windowSeconds + 1)
            redis.call('EXPIRE', key .. ':seq', windowSeconds + 1)
            return {1, maxRequests - (currentCount + 1), 0}
        else
            -- Find earliest timestamp in window to compute retry-after
            local earliest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
            local retryAfter = 0
            if #earliest >= 2 then
                local earliestTime = tonumber(earliest[2])
                retryAfter = (earliestTime + windowSeconds) - now
                if retryAfter < 1 then retryAfter = 1 end
            else
                retryAfter = windowSeconds
            end
            return {0, 0, retryAfter}
        end
    ";

    public RedisRateLimiter(
        IConnectionMultiplexer? redis,
        IRateLimiter fallbackLimiter,
        ILogger<RedisRateLimiter> logger)
    {
        _redis = redis;
        _fallbackLimiter = fallbackLimiter;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(
        string key,
        int maxRequests,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis is not connected. Using local in-memory fallback rate limiter for key {Key}.", key);
            return await _fallbackLimiter.CheckRateLimitAsync(key, maxRequests, window, cancellationToken);
        }

        try
        {
            var db = _redis.GetDatabase();
            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var windowSeconds = (long)Math.Ceiling(window.TotalSeconds);

            var redisResult = (RedisResult[]?)await db.ScriptEvaluateAsync(
                SlidingWindowLuaScript,
                new RedisKey[] { $"ratelimit:{key}" },
                new RedisValue[] { nowUnix, windowSeconds, maxRequests }
            );

            if (redisResult != null && redisResult.Length >= 3)
            {
                var isAllowed = (int)redisResult[0] == 1;
                var remaining = (int)redisResult[1];
                var retryAfterSeconds = (long)redisResult[2];

                return new RateLimitResult(
                    IsAllowed: isAllowed,
                    Remaining: remaining,
                    Limit: maxRequests,
                    RetryAfter: TimeSpan.FromSeconds(Math.Max(0, retryAfterSeconds))
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error communicating with Redis for rate limiting key {Key}. Falling back to memory limiter.", key);
            return await _fallbackLimiter.CheckRateLimitAsync(key, maxRequests, window, cancellationToken);
        }

        return await _fallbackLimiter.CheckRateLimitAsync(key, maxRequests, window, cancellationToken);
    }
}
