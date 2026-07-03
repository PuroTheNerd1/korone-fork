using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Roblox.ApiProxy.Configuration;
using Roblox.ApiProxy.Middleware;

namespace Roblox.ApiProxy.Tests;

public class AdminApiCorsMiddlewareTests
{
    [Fact]
    public async Task AdminHost_Preflight_AddsCredentialedCorsHeaders()
    {
        using var server = CreateServer();
        var request = new HttpRequestMessage(HttpMethod.Options, "/v1/users");
        request.Headers.Host = "admin.pekora.zip";
        request.Headers.Add("Origin", "https://www.pekora.zip");

        var response = await server.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("https://www.pekora.zip", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.Contains("x-csrf-token", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
        Assert.Contains("x-csrf-token", response.Headers.GetValues("Access-Control-Expose-Headers").Single());
    }

    [Fact]
    public async Task AdminHost_NonV1Path_ReturnsNotFoundBeforeFallback()
    {
        using var server = CreateServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Host = "admin.pekora.zip";

        var response = await client.GetAsync("/not-v1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NonAdminHost_ContinuesToNextMiddleware()
    {
        using var server = CreateServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Host = "www.pekora.zip";

        var response = await client.GetAsync("/not-v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("next", await response.Content.ReadAsStringAsync());
    }

    private static TestServer CreateServer()
    {
        return new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.Configure<AdminApiOptions>(options =>
                {
                    options.CorsAllowedOrigins = new[] { "https://www.pekora.zip" };
                });
            })
            .Configure(app =>
            {
                app.UseMiddleware<AdminApiCorsMiddleware>();
                app.Run(async context => await context.Response.WriteAsync("next"));
            }));
    }
}
