using BuildingBlocks.Infrastructure.RateLimiting;
using FluentAssertions;
using Xunit;

namespace HireFlow.Identity.UnitTests;

public class RateLimiterTests
{
    [Fact]
    public async Task CheckRateLimitAsync_ShouldAllowRequests_WithinMaxLimit()
    {
        // Arrange
        var limiter = new MemoryRateLimiter();
        var key = "test-client-1";
        var maxRequests = 3;
        var window = TimeSpan.FromMinutes(1);

        // Act & Assert
        var result1 = await limiter.CheckRateLimitAsync(key, maxRequests, window);
        result1.IsAllowed.Should().BeTrue();
        result1.Remaining.Should().Be(2);

        var result2 = await limiter.CheckRateLimitAsync(key, maxRequests, window);
        result2.IsAllowed.Should().BeTrue();
        result2.Remaining.Should().Be(1);

        var result3 = await limiter.CheckRateLimitAsync(key, maxRequests, window);
        result3.IsAllowed.Should().BeTrue();
        result3.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ShouldRejectRequests_WhenLimitExceeded()
    {
        // Arrange
        var limiter = new MemoryRateLimiter();
        var key = "test-client-reject";
        var maxRequests = 2;
        var window = TimeSpan.FromMinutes(1);

        // Act
        await limiter.CheckRateLimitAsync(key, maxRequests, window);
        await limiter.CheckRateLimitAsync(key, maxRequests, window);
        var excessResult = await limiter.CheckRateLimitAsync(key, maxRequests, window);

        // Assert
        excessResult.IsAllowed.Should().BeFalse();
        excessResult.Remaining.Should().Be(0);
        excessResult.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ShouldIsolateDifferentKeys()
    {
        // Arrange
        var limiter = new MemoryRateLimiter();
        var keyA = "ip-192.168.1.1";
        var keyB = "ip-10.0.0.1";
        var maxRequests = 1;
        var window = TimeSpan.FromMinutes(1);

        // Act
        var resultA1 = await limiter.CheckRateLimitAsync(keyA, maxRequests, window);
        var resultA2 = await limiter.CheckRateLimitAsync(keyA, maxRequests, window);
        var resultB1 = await limiter.CheckRateLimitAsync(keyB, maxRequests, window);

        // Assert
        resultA1.IsAllowed.Should().BeTrue();
        resultA2.IsAllowed.Should().BeFalse();
        resultB1.IsAllowed.Should().BeTrue(); // keyB is not affected by keyA
    }
}
