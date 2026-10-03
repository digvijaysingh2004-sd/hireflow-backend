using System.Collections.Concurrent;

namespace BuildingBlocks.Infrastructure.RateLimiting;

public sealed class MemoryRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> _requestTimestamps = new();

    public Task<RateLimitResult> CheckRateLimitAsync(
        string key,
        int maxRequests,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now - window;

        var queue = _requestTimestamps.GetOrAdd(key, _ => new ConcurrentQueue<DateTimeOffset>());

        lock (queue)
        {
            // Remove timestamps older than current sliding window
            while (queue.TryPeek(out var timestamp) && timestamp < windowStart)
            {
                queue.TryDequeue(out _);
            }

            if (queue.Count >= maxRequests)
            {
                // Oldest item determines retry-after
                var oldestInWindow = queue.TryPeek(out var oldest) ? oldest : windowStart;
                var retryAfter = (oldestInWindow + window) - now;
                if (retryAfter < TimeSpan.Zero) retryAfter = TimeSpan.FromSeconds(1);

                return Task.FromResult(new RateLimitResult(
                    IsAllowed: false,
                    Remaining: 0,
                    Limit: maxRequests,
                    RetryAfter: retryAfter
                ));
            }

            queue.Enqueue(now);
            var remaining = Math.Max(0, maxRequests - queue.Count);

            return Task.FromResult(new RateLimitResult(
                IsAllowed: true,
                Remaining: remaining,
                Limit: maxRequests,
                RetryAfter: TimeSpan.Zero
            ));
        }
    }
}
