using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Roblox.Services.Api.Controllers;
using Roblox.Web.Infrastructure.Metadata;

namespace Roblox.Services.Api.Tests;

public class VersionCompatibilityRouteTests
{
    private const string RccAuthorization = "ApiRouteTestRccAuthorization";

    private static readonly IReadOnlyList<VersionCompatibilityRouteCase> Routes = new List<VersionCompatibilityRouteCase>
    {
        new("/GetAllowedMD5Hashes"),
        new("/GetAllowedSecurityKeys"),
        new("/GetAllowedSecurityVersions"),
    };

    public static IEnumerable<object[]> VersionCompatibilityRoutes()
    {
        return Routes.Select(route => new object[] { route });
    }

    [Fact]
    public void RouteMatrix_CoversEveryVersionCompatibilityControllerRoute()
    {
        var declaredRoutes = typeof(VersionCompatibilityController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(attribute => attribute.Template == null
                ? Enumerable.Empty<(string Method, string Path)>()
                : attribute.HttpMethods.Select(method => (Method: method.ToUpperInvariant(), Path: NormalizeRoute(attribute.Template))))
            .ToHashSet();

        var matrixRoutes = Routes
            .Select(route => (Method: "GET", Path: route.Path))
            .ToHashSet();

        var missing = declaredRoutes.Except(matrixRoutes).OrderBy(route => route.Path).ToList();
        var extra = matrixRoutes.Except(declaredRoutes).OrderBy(route => route.Path).ToList();

        Assert.True(missing.Count == 0, "Missing version compatibility route matrix entries: " + string.Join(", ", missing));
        Assert.True(extra.Count == 0, "Version compatibility route matrix contains entries not declared by controller: " + string.Join(", ", extra));
    }

    [Fact]
    public void RouteMatrix_AllVersionCompatibilityRoutesRequireRcc()
    {
        var unprotectedRoutes = typeof(VersionCompatibilityController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<RequireRccRequestAttribute>() == null)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .Select(attribute => NormalizeRoute(attribute.Template ?? string.Empty))
            .ToList();

        Assert.Empty(unprotectedRoutes);
    }

    [Theory]
    [MemberData(nameof(VersionCompatibilityRoutes))]
    public async Task VersionCompatibilityRoutes_RejectAnonymousRequests(VersionCompatibilityRouteCase route)
    {
        await using var factory = new ApiServiceFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync(route.Path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllowedMd5Hashes_WithRcc_ReturnsAllowedHashes()
    {
        await using var factory = new ApiServiceFactory();
        using var client = CreateRccClient(factory);

        var response = await client.GetAsync("/GetAllowedMD5Hashes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var hashes = json.RootElement.GetProperty("data").EnumerateArray().Select(hash => hash.GetString()).ToList();
        Assert.Equal(new[]
        {
            "088e8d2d5d31fd351f66efc7049dab10",
            "bba43f967698feff49038f51b391b48e",
            "4091ce1193a5430573430411eb20bd44",
            "7da7086e7f3a739873fa5970ef586e98",
            "1fd6e7becff68acc140b2db17e24c86e",
        }, hashes);
    }

    [Fact]
    public async Task GetAllowedSecurityKeys_WithRcc_ReturnsTrue()
    {
        await using var factory = new ApiServiceFactory();
        using var client = CreateRccClient(factory);

        var response = await client.GetAsync("/GetAllowedSecurityKeys");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("true", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetAllowedSecurityVersions_WithRcc_ReturnsSerializedVersionList()
    {
        await using var factory = new ApiServiceFactory();
        using var client = CreateRccClient(factory);

        var response = await client.GetAsync("/GetAllowedSecurityVersions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var versionsJson = json.RootElement.GetProperty("data").GetString();
        Assert.NotNull(versionsJson);

        var versions = JsonSerializer.Deserialize<string[]>(versionsJson!);
        Assert.Equal(new[]
        {
            "0.206.0pcplayer",
            "0.235.0pcplayer",
            "0.314.0pcplayer",
            "0.376.0pcplayer",
            "0.355.0pcplayer",
            "2.355.0iosapp",
            "0.395.0pcplayer",
            "0.450.0pcplayer",
            "0.451.0pcplayer",
            "0.463.0pcplayer",
        }, versions);
    }

    private static HttpClient CreateRccClient(ApiServiceFactory factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("accesskey", RccAuthorization);
        return client;
    }

    private static string NormalizeRoute(string route)
    {
        return "/" + route.Trim('/');
    }

    public sealed record VersionCompatibilityRouteCase(string Path)
    {
        public override string ToString()
        {
            return $"GET {Path}";
        }
    }

    private sealed class ApiServiceFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("Postgres", " ");
            Environment.SetEnvironmentVariable("Redis", " ");
            Environment.SetEnvironmentVariable("Authorization", "ApiRouteTestAuthorization");
            Environment.SetEnvironmentVariable("RccAuthorization", RccAuthorization);
            builder.UseEnvironment("Testing");
        }
    }
}
