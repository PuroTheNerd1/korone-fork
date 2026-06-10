using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Roblox.Services.Api.Controllers;
using Roblox.Web.Infrastructure.Metadata;

namespace Roblox.Services.Api.Tests;

public class UniversesRouteTests
{
    private static readonly IReadOnlyList<UniversesRouteCase> Routes = new List<UniversesRouteCase>
    {
        new("GET", "/universes/get-universe-containing-place", false),
        new("GET", "/universes/get-info", true),
        new("GET", "/universes/get-universe-places", true),
        new("GET", "/universes/get-aliases", false),
    };

    public static IEnumerable<object[]> SessionRoutes()
    {
        return Routes
            .Where(route => route.RequiresSession)
            .Select(route => new object[] { route });
    }

    [Fact]
    public void RouteMatrix_CoversEveryUniversesControllerRoute()
    {
        var declaredRoutes = typeof(UniversesController)
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

        Assert.True(missing.Count == 0, "Missing Universes route matrix entries: " + string.Join(", ", missing));
        Assert.True(extra.Count == 0, "Universes route matrix contains entries not declared by controller: " + string.Join(", ", extra));
    }

    [Fact]
    public void RouteMatrix_MatchesExplicitSessionMetadata()
    {
        var declaredRoutes = typeof(UniversesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method =>
            {
                var requiresSession = method.GetCustomAttribute<RequireRobloxSessionAttribute>() != null;
                return method.GetCustomAttributes<HttpMethodAttribute>()
                    .SelectMany(attribute => attribute.Template == null
                        ? Enumerable.Empty<(string Method, string Path, bool RequiresSession)>()
                        : attribute.HttpMethods.Select(httpMethod => (
                            Method: httpMethod.ToUpperInvariant(),
                            Path: NormalizeRoute(attribute.Template),
                            RequiresSession: requiresSession)));
            })
            .ToDictionary(route => (route.Method, route.Path));

        foreach (var route in Routes)
        {
            Assert.True(declaredRoutes.TryGetValue((route.Method, route.Path), out var declared), $"Route {route} is not declared.");
            Assert.Equal(route.RequiresSession, declared.RequiresSession);
        }
    }

    [Theory]
    [MemberData(nameof(SessionRoutes))]
    public async Task SessionUniverseRoutes_RejectAnonymousRequests(UniversesRouteCase route)
    {
        await using var factory = new ApiServiceFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync(route.Path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string NormalizeRoute(string route)
    {
        return "/" + route.Trim('/');
    }

    public sealed record UniversesRouteCase(string Method, string Path, bool RequiresSession)
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
