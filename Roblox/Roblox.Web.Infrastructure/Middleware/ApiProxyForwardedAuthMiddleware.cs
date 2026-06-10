using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Roblox.Web.Infrastructure.Auth;
using Roblox.Web.Infrastructure.Configuration;
using Roblox.Web.Infrastructure.Http;

namespace Roblox.Web.Infrastructure.Middleware;

public class ApiProxyForwardedAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RobloxWebInfrastructureOptions _options;

    public ApiProxyForwardedAuthMiddleware(RequestDelegate next, IOptions<RobloxWebInfrastructureOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, IRobloxRequestContextAccessor requestContextAccessor)
    {
        if (!ShouldDecorateRequest(context.Request.Host.Host, context.Request.Path))
        {
            await _next(context);
            return;
        }

        var requestContext = RobloxRequestContextFactory.CreateAnonymous(context, _options.RccAuthorization);
        var resolvedSession = await RobloxSessionResolver.TryResolveFromCookie(context);
        if (resolvedSession != null)
        {
            requestContext = RobloxRequestContextFactory.CreateWithSession(context, resolvedSession.Session, _options.RccAuthorization);
        }

        requestContext.IsTrustedInternalRequest = true;
        requestContextAccessor.SetCurrent(requestContext);

        if (!string.IsNullOrWhiteSpace(_options.Authorization))
        {
            context.Request.Headers[RobloxWebContextConstants.ProxyAuthorizationHeaderName] = _options.Authorization;
        }

        context.Request.Headers[RobloxWebContextConstants.ClientIpHashHeaderName] = requestContext.HashedIp;
        context.Request.Headers[RobloxWebContextConstants.UserAgentHeaderName] = requestContext.UserAgent;
        context.Request.Headers[RobloxWebContextConstants.GameIdHeaderName] = requestContext.CurrentGameId;
        context.Request.Headers[RobloxWebContextConstants.PlaceIdHeaderName] = requestContext.CurrentPlaceId.ToString();
        context.Request.Headers[RobloxWebContextConstants.AuthTypeHeaderName] = requestContext.IsRcc
            ? "rcc"
            : requestContext.IsRobloxClient ? "roblox" : "browser";

        if (requestContext.Session != null)
        {
            context.Request.Headers[RobloxWebContextConstants.UserIdHeaderName] = requestContext.Session.userId.ToString();
            context.Request.Headers[RobloxWebContextConstants.UsernameHeaderName] = requestContext.Session.username;
            context.Request.Headers[RobloxWebContextConstants.SessionIdHeaderName] = requestContext.Session.sessionId;
            context.Request.Headers[RobloxWebContextConstants.AccountStatusHeaderName] = requestContext.Session.accountStatus.ToString();
        }

        await _next(context);
    }

    private bool ShouldDecorateRequest(string host, PathString path)
    {
        if (_options.InternalServiceRoutes.Any(route => RouteMatches(route, host, path)))
        {
            return true;
        }

        return _options.InternalServiceHosts.Any(candidate => string.Equals(candidate, host, StringComparison.OrdinalIgnoreCase));
    }

    private static bool RouteMatches(RobloxInternalServiceRoute route, string host, PathString path)
    {
        var hostMatches = route.Hosts.Count == 0 || route.Hosts.Any(candidate => string.Equals(candidate, host, StringComparison.OrdinalIgnoreCase));
        if (!hostMatches)
        {
            return false;
        }

        if (route.PathPrefixes.Count == 0)
        {
            return true;
        }

        return route.PathPrefixes.Any(prefix => path.StartsWithSegments(NormalizePathPrefix(prefix), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePathPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return "/";
        }

        if (!prefix.StartsWith('/'))
        {
            prefix = "/" + prefix;
        }

        return prefix.Length > 1 ? prefix.TrimEnd('/') : prefix;
    }
}
