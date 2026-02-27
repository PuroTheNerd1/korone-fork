using System.Threading.RateLimiting;
using Roblox.Exceptions;
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

            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                throw new TooManyRequestsException(0, "Too many requests");
            };
        });

        return services;
    }
}
