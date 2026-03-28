using System.Threading.RateLimiting;
using Roblox.Website.Controllers;
using Roblox.Website.WebsiteModels;

namespace Roblox.Website.Middleware;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddRobloxRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                string key;
                try
                {
                    key = ControllerBase.GetIP(ControllerBase.GetRequesterIpRaw(ctx));
                }
                catch
                {
                    key = "unknown";
                }

                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromSeconds(60),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    errors = new[] { new ErrorResponseEntry { code = 0, message = "Too many requests" } }
                }, cancellationToken);
            };
        });

        return services;
    }
}
