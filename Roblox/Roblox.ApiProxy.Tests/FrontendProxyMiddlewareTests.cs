using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Roblox.ApiProxy.Configuration;
using Roblox.ApiProxy.Middleware;
using Roblox.Models.Sessions;
using Roblox.Models.Users;
using Roblox.Web.Infrastructure.Http;
using Yarp.ReverseProxy.Forwarder;

namespace Roblox.ApiProxy.Tests;

public class FrontendProxyMiddlewareTests
{
    [Fact]
    public async Task FrontendPath_WithoutSession_RedirectsToRoot()
    {
        var (context, forwarder) = await InvokeAsync();

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/", context.Response.Headers.Location.ToString());
        Assert.Equal(0, forwarder.ForwardCount);
    }

    [Theory]
    [InlineData(AccountStatus.Suppressed)]
    [InlineData(AccountStatus.Poisoned)]
    [InlineData(AccountStatus.Deleted)]
    public async Task FrontendPath_WithNotApprovedSession_RedirectsToNotApproved(AccountStatus accountStatus)
    {
        var (context, forwarder) = await InvokeAsync(CreateSession(accountStatus));

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/auth/notapproved", context.Response.Headers.Location.ToString());
        Assert.Equal(0, forwarder.ForwardCount);
    }

    [Fact]
    public async Task FrontendPath_WithOkSession_ForwardsToFrontend()
    {
        var (context, forwarder) = await InvokeAsync(CreateSession(AccountStatus.Ok));

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("forwarded", await ReadBodyAsync(context));
        Assert.Equal(1, forwarder.ForwardCount);
    }

    private static async Task<(DefaultHttpContext Context, FakeForwarder Forwarder)> InvokeAsync(UserSession? session = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Host = new HostString("www.pekora.zip");
        context.Request.Path = "/home";
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.SetRobloxRequestContext(new RobloxRequestContext
        {
            Session = session,
            IsAuthenticated = session != null,
        });

        var forwarder = new FakeForwarder();
        var middleware = new FrontendProxyMiddleware(
            _ => Task.CompletedTask,
            forwarder,
            Options.Create(new FrontendProxyOptions
            {
                DestinationPrefix = "http://127.0.0.1:3000/",
                PublicHosts = new[] { "www.pekora.zip" },
            }),
            NullLogger<FrontendProxyMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        return (context, forwarder);
    }

    private static UserSession CreateSession(AccountStatus accountStatus)
    {
        return new UserSession(123, "FrontendUser", DateTime.UtcNow, accountStatus, 0, false, "session-id");
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private sealed class FakeForwarder : IHttpForwarder
    {
        public int ForwardCount { get; private set; }

        public ValueTask<ForwarderError> SendAsync(
            HttpContext context,
            string destinationPrefix,
            HttpMessageInvoker httpClient,
            ForwarderRequestConfig requestConfig,
            HttpTransformer transformer)
        {
            return SendAsync(context, destinationPrefix, httpClient, requestConfig, transformer, CancellationToken.None);
        }

        public async ValueTask<ForwarderError> SendAsync(
            HttpContext context,
            string destinationPrefix,
            HttpMessageInvoker httpClient,
            ForwarderRequestConfig requestConfig,
            HttpTransformer transformer,
            CancellationToken cancellationToken)
        {
            ForwardCount++;
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("forwarded", cancellationToken);
            return ForwarderError.None;
        }
    }
}
