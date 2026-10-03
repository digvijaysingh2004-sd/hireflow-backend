using System.Security.Claims;
using System.Text.Json;
using BuildingBlocks.Infrastructure.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Idempotency;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    public int WindowSeconds { get; set; } = 86400; // Default 24 hours
    public string HeaderName { get; set; } = "Idempotency-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var cacheService = httpContext.RequestServices.GetRequiredService<ICacheService>();

        // Check for idempotency key in headers
        string? idempotencyKey = null;
        if (httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValues) ||
            httpContext.Request.Headers.TryGetValue("X-Idempotency-Key", out headerValues))
        {
            idempotencyKey = headerValues.FirstOrDefault()?.Trim();
        }

        // If no idempotency key provided, proceed normally
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next();
            return;
        }

        var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var cacheKey = $"idempotency:{userId}:{httpContext.Request.Method}:{httpContext.Request.Path}:{idempotencyKey}";

        // 1. Check if response is already cached
        var existingResponse = await cacheService.GetAsync<CachedHttpResponse>(cacheKey, httpContext.RequestAborted);
        if (existingResponse != null)
        {
            context.Result = new ContentResult
            {
                StatusCode = existingResponse.StatusCode,
                ContentType = existingResponse.ContentType ?? "application/json",
                Content = existingResponse.BodyJson
            };
            return;
        }

        // 2. Execute the action
        var executedContext = await next();

        // 3. Cache successful or client responses (2xx / 4xx) so retries are identical
        if (executedContext.Result is ObjectResult objectResult && objectResult.StatusCode is >= 200 and < 500)
        {
            var bodyJson = JsonSerializer.Serialize(objectResult.Value);
            var cachedResponse = new CachedHttpResponse(
                StatusCode: objectResult.StatusCode ?? 200,
                ContentType: "application/json",
                BodyJson: bodyJson
            );

            await cacheService.SetAsync(
                cacheKey,
                cachedResponse,
                TimeSpan.FromSeconds(WindowSeconds),
                httpContext.RequestAborted
            );
        }
    }
}

public sealed record CachedHttpResponse(
    int StatusCode,
    string? ContentType,
    string BodyJson
);
