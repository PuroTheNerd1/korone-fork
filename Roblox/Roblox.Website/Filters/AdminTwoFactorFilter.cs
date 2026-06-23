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

    public static string GetKey(long userId, string sessionId) => redisKey + userId + ":" + sessionId;

    public static async Task<bool> IsVerified(long userId, string sessionId)
    {
        var val = await redis.StringGetAsync(GetKey(userId, sessionId));
        return val != null;
    }

    public static async Task MarkVerified(long userId, string sessionId)
    {
        await redis.StringSetAsync(GetKey(userId, sessionId), "1");
    }
    public static async Task Invalidate(long userId, string sessionId)
    {
        await redis.KeyDeleteAsync(GetKey(userId, sessionId));
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
        if (session == null || !await IsVerified(session.userId, session.sessionId))
        {
            var isApi = context.HttpContext.Request.Path.StartsWithSegments("/admin-api");
            if (isApi)
                context.Result = new JsonResult(new { error = "2FA verification required" }) { StatusCode = 401 };
            else
                context.Result = new RedirectResult("/admin/2fa");
            return;
        }
        await next();
    }
}