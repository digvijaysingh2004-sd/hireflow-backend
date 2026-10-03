namespace BuildingBlocks.Infrastructure.RateLimiting;

public sealed record RateLimitResult(
    bool IsAllowed,
    int Remaining,
    int Limit,
    TimeSpan RetryAfter
);

public interface IRateLimiter
{
    Task<RateLimitResult> CheckRateLimitAsync(
        string key,
        int maxRequests,
        TimeSpan window,
        CancellationToken cancellationToken = default);
}
