using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Roblox.Services.Api.Controllers;
using Roblox.Web.Infrastructure.Metadata;

namespace Roblox.Services.Api.Tests;

public class CurrencyRouteTests
{
    private static readonly IReadOnlyList<CurrencyRouteCase> Routes = new List<CurrencyRouteCase>
    {
        new("GET", "/currency/balance", true),
    };

    [Fact]
    public void RouteMatrix_CoversEveryCurrencyControllerRoute()
    {
        var declaredRoutes = typeof(CurrencyController)
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

        Assert.True(missing.Count == 0, "Missing Currency route matrix entries: " + string.Join(", ", missing));
        Assert.True(extra.Count == 0, "Currency route matrix contains entries not declared by controller: " + string.Join(", ", extra));
    }

    [Fact]
    public void RouteMatrix_MatchesExplicitSessionMetadata()
    {
        var method = typeof(CurrencyController).GetMethod(nameof(CurrencyController.GetBalance));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<RequireRobloxSessionAttribute>());
    }

    [Fact]
    public async Task BalanceRoute_RejectsAnonymousRequests()
    {
        await using var factory = new ApiServiceFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/currency/balance");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string NormalizeRoute(string route)
    {
        return "/" + route.Trim('/');
    }

    public sealed record CurrencyRouteCase(string Method, string Path, bool RequiresSession);

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
