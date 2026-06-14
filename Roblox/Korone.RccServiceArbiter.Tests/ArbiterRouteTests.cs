using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Korone.RccServiceArbiter.Controllers;
using Korone.RccServiceArbiter.Models;
using Korone.RccServiceArbiter.Processes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Roblox.Web.Infrastructure.Metadata;
using Xunit;

namespace Korone.RccServiceArbiter.Tests;

public sealed class ArbiterRouteTests
{
    private static readonly IReadOnlyList<RouteCase> Routes = new List<RouteCase>
    {
        new("GET", "/version"),
        new("GET", "/get-all-game-servers"),
        new("POST", "/start-game-server"),
        new("POST", "/kill-game-server"),
        new("POST", "/evict-player"),
        new("POST", "/set-filtering-enabled"),
        new("POST", "/clean-up"),
    };

    public static IEnumerable<object[]> ArbiterRoutes()
    {
        return Routes.Select(route => new object[] { route });
    }

    [Fact]
    public void RouteMatrix_CoversEveryArbiterControllerRoute()
    {
        var declaredRoutes = typeof(ArbiterController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(attribute => attribute.Template == null
                ? Enumerable.Empty<(string Method, string Path)>()
                : attribute.HttpMethods.Select(method => (Method: method.ToUpperInvariant(), Path: NormalizeRoute(attribute.Template))))
            .ToHashSet();

        var matrixRoutes = Routes.Select(route => (route.Method, route.Path)).ToHashSet();

        Assert.Empty(declaredRoutes.Except(matrixRoutes));
        Assert.Empty(matrixRoutes.Except(declaredRoutes));
    }

    [Fact]
    public void ArbiterController_DeclaresInternalServiceMetadata()
    {
        Assert.NotNull(typeof(ArbiterController).GetCustomAttribute<InternalServiceOnlyAttribute>());
    }

    [Theory]
    [MemberData(nameof(ArbiterRoutes))]
    public async Task ArbiterRoutes_RejectAnonymousRequests(RouteCase route)
    {
        await using var factory = new ArbiterFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.SendAsync(CreateRequest(route));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StartGameServer_AuthorizedRequest_ReturnsAllocatedPorts()
    {
        await using var factory = new ArbiterFactory();
        using var client = factory.CreateAuthorizedClient();
        var jobId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/start-game-server", new StartGameServerRequest
        {
            JobId = jobId,
            PlaceId = 123,
            UniverseId = 456,
            MaxPlayerCount = 12,
            CreatorId = 789,
            PlaceVersion = 1,
            MatchmakingContextId = 1,
            Year = 2021,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<StartGameServerResponse>();
        Assert.NotNull(payload);
        Assert.Equal(jobId, payload.JobId);
        Assert.Equal(45001, payload.RccPort);
        Assert.Equal(50001, payload.GameServerPort);
        Assert.Equal(30001, payload.ProxyPort);
        Assert.Equal("Started", payload.Status);
    }

    [Fact]
    public async Task ActionRoutes_AuthorizedRequests_ReturnExpectedShape()
    {
        await using var factory = new ArbiterFactory();
        using var client = factory.CreateAuthorizedClient();
        var jobId = Guid.NewGuid();

        var kill = await client.PostAsJsonAsync("/kill-game-server", new KillGameServerRequest { JobId = jobId });
        var evict = await client.PostAsJsonAsync("/evict-player", new EvictPlayerRequest { GameId = jobId, UserId = 12, MessageVersionId = 1 });
        var filtering = await client.PostAsJsonAsync("/set-filtering-enabled", new SetFilteringEnabledRequest { JobId = jobId, IsEnabled = true });
        var cleanup = await client.PostAsync("/clean-up", null);
        var stats = await client.GetAsync("/get-all-game-servers");

        Assert.True((await kill.Content.ReadFromJsonAsync<ArbiterActionResponse>())!.Success);
        Assert.True((await evict.Content.ReadFromJsonAsync<ArbiterActionResponse>())!.Success);
        Assert.True((await filtering.Content.ReadFromJsonAsync<ArbiterActionResponse>())!.Success);
        Assert.Equal(HttpStatusCode.OK, cleanup.StatusCode);
        Assert.Equal(HttpStatusCode.OK, stats.StatusCode);
    }

    private static HttpRequestMessage CreateRequest(RouteCase route)
    {
        var request = new HttpRequestMessage(new HttpMethod(route.Method), route.Path);
        if (route.Method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        return request;
    }

    private static string NormalizeRoute(string route)
    {
        return "/" + route.Trim('/');
    }

    public sealed record RouteCase(string Method, string Path);

    private sealed class ArbiterFactory : WebApplicationFactory<Program>
    {
        public HttpClient CreateAuthorizedClient()
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            client.DefaultRequestHeaders.Add("rblx-authorization", "ArbiterRouteTestAuthorization");
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("Postgres", " ");
            Environment.SetEnvironmentVariable("Redis", " ");
            Environment.SetEnvironmentVariable("Authorization", "ArbiterRouteTestAuthorization");
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRccProcessPool>();
                services.RemoveAll<IHostedService>();
                services.AddSingleton<IRccProcessPool, FakeProcessPool>();
            });
        }
    }

    private sealed class FakeProcessPool : IRccProcessPool
    {
        public Task<StartGameServerResponse> StartGameServerAsync(StartGameServerRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StartGameServerResponse
            {
                JobId = request.JobId,
                RccPort = 45001,
                GameServerPort = 50001,
                ProxyPort = 30001,
                RccProcessId = 101,
                QuilkinProcessId = 202,
            });
        }

        public Task<bool> StopGameServerAsync(Guid jobId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> EvictPlayerAsync(Guid jobId, long userId, int messageVersionId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> SetFilteringEnabledAsync(Guid jobId, bool isEnabled, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> RunGlobalMessageAsync(Guid jobId, string topic, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<int> CleanUpAsync(CancellationToken cancellationToken) => Task.FromResult(1);

        public ArbiterStatisticsResponse GetStatistics()
        {
            return new ArbiterStatisticsResponse
            {
                ServerCount = 0,
            };
        }
    }
}
