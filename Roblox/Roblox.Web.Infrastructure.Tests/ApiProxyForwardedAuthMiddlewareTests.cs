using Microsoft.AspNetCore.Http;
using Roblox.Web.Infrastructure.Configuration;
using Roblox.Web.Infrastructure.Http;

namespace Roblox.Web.Infrastructure.Tests;

public class ApiProxyForwardedAuthMiddlewareTests
{
    [Fact]
    public async Task DoesNotDecorateUnmatchedHostOrRoute()
    {
        var (context, nextCalled) = await InfrastructureTestHelpers.InvokeApiProxyForwardedAuthAsync(options =>
        {
            options.InternalServiceHosts.Add("internal.test.local");
            options.InternalServiceRoutes.Add(new RobloxInternalServiceRoute
            {
                Hosts = new List<string> { "api.test.local" },
                PathPrefixes = new List<string> { "/internal" },
            });
        });

        Assert.True(nextCalled);
        Assert.False(context.Request.Headers.ContainsKey(RobloxWebContextConstants.ProxyAuthorizationHeaderName));
        Assert.Null(context.GetRobloxRequestContext());
    }

    [Fact]
    public async Task DecoratesConfiguredHostWithProxyHeadersAndTrustedContext()
    {
        var (context, nextCalled) = await InfrastructureTestHelpers.InvokeApiProxyForwardedAuthAsync(
            options => options.InternalServiceHosts.Add("WWW.TEST.LOCAL"),
            ctx =>
            {
                ctx.Request.Headers.UserAgent = "Roblox/WinInet";
                ctx.Request.Headers[RobloxWebContextConstants.ClientIpHashHeaderName] = "client-hash";
                ctx.Request.Headers[RobloxWebContextConstants.GameIdHeaderName] = "game-1";
                ctx.Request.Headers[RobloxWebContextConstants.PlaceIdHeaderName] = "999";
            });

        Assert.True(nextCalled);
        Assert.Equal(TestConstants.ProxyAuthorization, context.Request.Headers[RobloxWebContextConstants.ProxyAuthorizationHeaderName]);
        Assert.Equal("client-hash", context.Request.Headers[RobloxWebContextConstants.ClientIpHashHeaderName]);
        Assert.Equal("Roblox/WinInet", context.Request.Headers[RobloxWebContextConstants.UserAgentHeaderName]);
        Assert.Equal("game-1", context.Request.Headers[RobloxWebContextConstants.GameIdHeaderName]);
        Assert.Equal("999", context.Request.Headers[RobloxWebContextConstants.PlaceIdHeaderName]);
        Assert.Equal("roblox", context.Request.Headers[RobloxWebContextConstants.AuthTypeHeaderName]);
        Assert.True(context.GetRobloxRequestContext()!.IsTrustedInternalRequest);
    }

    [Fact]
    public async Task DecoratesConfiguredRoutePrefixCaseInsensitively()
    {
        var (context, nextCalled) = await InfrastructureTestHelpers.InvokeApiProxyForwardedAuthAsync(
            options => options.InternalServiceRoutes.Add(new RobloxInternalServiceRoute
            {
                Hosts = new List<string> { "www.test.local" },
                PathPrefixes = new List<string> { "/Internal/Avatar" },
            }),
            ctx => ctx.Request.Path = "/internal/avatar/v1/test");

        Assert.True(nextCalled);
        Assert.Equal(TestConstants.ProxyAuthorization, context.Request.Headers[RobloxWebContextConstants.ProxyAuthorizationHeaderName]);
        Assert.True(context.GetRobloxRequestContext()!.IsTrustedInternalRequest);
    }

    [Fact]
    public async Task DecoratesRccAuthTypeWhenAccessKeyIsValid()
    {
        var (context, nextCalled) = await InfrastructureTestHelpers.InvokeApiProxyForwardedAuthAsync(
            options => options.InternalServiceHosts.Add("www.test.local"),
            ctx => ctx.Request.Headers["accesskey"] = TestConstants.RccAuthorization);

        Assert.True(nextCalled);
        Assert.Equal("rcc", context.Request.Headers[RobloxWebContextConstants.AuthTypeHeaderName]);
    }
}
