using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.RateLimiting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class RateLimitAttribute : Attribute, IAsyncActionFilter
{
    public int MaxRequests { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
    public string Policy { get; set; } = "default";
    public bool IncludeUserId { get; set; } = false;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var rateLimiter = httpContext.RequestServices.GetRequiredService<IRateLimiter>();

        // Build rate limiting key: policy + client IP (+ optional user ID)
        var clientIp = GetClientIpAddress(httpContext);
        var keyBuilder = new StringBuilder($"{Policy}:{clientIp}");

        if (IncludeUserId)
        {
            var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                keyBuilder.Append($":user:{userId}");
            }
        }

        var key = keyBuilder.ToString();
        var window = TimeSpan.FromSeconds(WindowSeconds);

        var result = await rateLimiter.CheckRateLimitAsync(key, MaxRequests, window, httpContext.RequestAborted);

        // Always attach standard rate-limit headers to response
        httpContext.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString(CultureInfo.InvariantCulture);
        httpContext.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString(CultureInfo.InvariantCulture);

        if (!result.IsAllowed)
        {
            var retryAfterSec = Math.Max(1, (int)Math.Ceiling(result.RetryAfter.TotalSeconds));
            httpContext.Response.Headers["Retry-After"] = retryAfterSec.ToString(CultureInfo.InvariantCulture);

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc6585#section-4",
                Title = "Too Many Requests",
                Status = StatusCodes.Status429TooManyRequests,
                Detail = $"Rate limit exceeded for policy '{Policy}'. Please retry after {retryAfterSec} seconds.",
                Instance = httpContext.Request.Path
            };

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
            return;
        }

        await next();
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip)) return ip;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}
