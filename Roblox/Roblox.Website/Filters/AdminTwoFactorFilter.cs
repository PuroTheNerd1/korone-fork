using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Roblox.Cache;
using Roblox.Models.Sessions;
using Roblox.Website.Middleware;
using Roblox.Website.WebsiteModels;

[AttributeUsage(AttributeTargets.Method)]
public class SkipAdminTwoFactorAttribute : Attribute { };

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminTwoFactorFilter : Attribute, IAsyncActionFilter
{
    private static DistributedCache redis => Roblox.Services.Cache.distributed;

    private const string redisKey = "admin:2fa:v1:";
    private static readonly TimeSpan ttl = TimeSpan.FromMinutes(20);

    public static string GetKey(long userId) => redisKey + userId;

    public static async Task<bool> IsVerified(long userId)
    {
        var val = await redis.StringGetAsync(GetKey(userId));
        return val != null;
    }

    public static async Task MarkVerified(long userId)
    {
        await redis.StringSetAsync(GetKey(userId), "1");
    }
    public static async Task Invalidate(long userId)
    {
        await redis.KeyDeleteAsync(GetKey(userId));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var skip = context.ActionDescriptor.EndpointMetadata.OfType<SkipAdminTwoFactorAttribute>().Any();
        if (skip)
        {
            await next();
            return;
        }

        var session = context.HttpContext.Items[SessionMiddleware.CookieName] as UserSession;
        if (session == null || !await IsVerified(session.userId))
        {
            var isApi = context.HttpContext.Request.Path.StartsWithSegments("/admin");
            if (isApi)
                context.Result = new JsonResult(new { error = "2FA verification required" }) { StatusCode = 401 };
            else
                context.Result = new RedirectResult("/admin/2fa");
            return;
        }

    }
}