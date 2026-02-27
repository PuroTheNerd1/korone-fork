using System.Threading.RateLimiting;
using Roblox.Website.Controllers;

namespace Roblox.Website.Middleware;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddRobloxRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                var rawIp = ControllerBase.GetRequesterIpRaw(ctx);
                var key = ControllerBase.GetIP(rawIp);
                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromSeconds(60),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    errors = new[]
                    {
                        new { code = 429, message = "Too many requests. Please wait before trying again." }
                    }
                }, cancellationToken);
            };
        });

        return services;
    }
}
