using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Roblox.Web.Infrastructure.Configuration;
using Roblox.Web.Infrastructure.Http;
using Roblox.Web.Infrastructure.Metadata;

namespace Roblox.Web.Infrastructure.Middleware;

public class ProxyForwardedAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RobloxWebInfrastructureOptions _options;

    public ProxyForwardedAuthMiddleware(RequestDelegate next, IOptions<RobloxWebInfrastructureOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, IRobloxRequestContextAccessor requestContextAccessor)
    {
        var endpoint = context.GetEndpoint();
        var allowAnonymous = endpoint.AllowsRobloxAnonymous() || endpoint.IsBrowserFacingEndpoint();
        var requiresSession = endpoint.RequiresRobloxSession();
        var isAuthorized = IsAuthorized(context);

        if (!isAuthorized && !allowAnonymous)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                errors = new[]
                {
                    new { code = 0, message = "Unauthorized" },
                },
            });
            return;
        }

        var requestContext = RobloxRequestContextFactory.CreateFromForwardedHeaders(context, isAuthorized);
        requestContextAccessor.SetCurrent(requestContext);

        if (requiresSession && !requestContext.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                errors = new[]
                {
                    new { code = 0, message = "Unauthorized" },
                },
            });
            return;
        }

        await _next(context);
    }

    private bool IsAuthorized(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(_options.Authorization))
        {
            return false;
        }

        return context.Request.Headers.TryGetValue(RobloxWebContextConstants.ProxyAuthorizationHeaderName, out var provided) &&
               provided.ToString() == _options.Authorization;
    }
}
