using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Roblox.Services.Api.Controllers;
using Roblox.Web.Infrastructure.Metadata;

namespace Roblox.Services.Api.Tests;

public class PresenceRouteTests
{
    private static readonly IReadOnlyList<PresenceRouteCase> Routes = new List<PresenceRouteCase>
    {
        new("POST", "/presence/register-game-presence"),
        new("POST", "/presence/register-absence"),
    };

    public static IEnumerable<object[]> PresenceRoutes()
    {
        return Routes.Select(route => new object[] { route });
    }

    [Fact]
    public void RouteMatrix_CoversEveryPresenceControllerRoute()
    {
        var declaredRoutes = typeof(PresenceController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(attribute => attribute.Template == null
                ? Enumerable.Empty<(string Method, string Path)>()
                : attribute.HttpMethods.Select(method => (Method: method.ToUpperInvariant(), Path: NormalizeRoute(attribute.Template))))
            .ToHashSet();

        var matrixRoutes = Routes
            .Select(route => (Method: route.Method, Path: route.Path))
            .ToHashSet();

        var missing = declaredRoutes.Except(matrixRoutes).OrderBy(route => route.Method).ThenBy(route => route.Path).ToList();
        var extra = matrixRoutes.Except(declaredRoutes).OrderBy(route => route.Method).ThenBy(route => route.Path).ToList();

        Assert.True(missing.Count == 0, "Missing Presence route matrix entries: " + string.Join(", ", missing));
        Assert.True(extra.Count == 0, "Presence route matrix contains entries not declared by controller: " + string.Join(", ", extra));
    }

    [Fact]
    public void RouteMatrix_AllPresenceRoutesRequireRcc()
    {
        var unprotectedRoutes = typeof(PresenceController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<RequireRccRequestAttribute>() == null)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .Select(attribute => NormalizeRoute(attribute.Template ?? string.Empty))
            .ToList();

        Assert.Empty(unprotectedRoutes);
    }

    [Theory]
    [MemberData(nameof(PresenceRoutes))]
    public async Task PresenceRoutes_RejectAnonymousRequests(PresenceRouteCase route)
    {
        await using var factory = new ApiServiceFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(route.Method), route.Path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string NormalizeRoute(string route)
    {
        return "/" + route.Trim('/');
    }

    public sealed record PresenceRouteCase(string Method, string Path)
    {
        public override string ToString()
        {
            return $"{Method} {Path}";
        }
    }

    private sealed class ApiServiceFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("Postgres", " ");
            Environment.SetEnvironmentVariable("Redis", " ");
            Environment.SetEnvironmentVariable("Authorization", "ApiRouteTestAuthorization");
            Environment.SetEnvironmentVariable("RccAuthorization", "ApiRouteTestRccAuthorization");
            builder.UseEnvironment("Testing");
        }
    }
}
