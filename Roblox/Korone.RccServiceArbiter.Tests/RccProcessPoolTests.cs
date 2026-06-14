using Korone.RccServiceArbiter.Configuration;
using Korone.RccServiceArbiter.Models;
using Korone.RccServiceArbiter.Processes;
using Korone.RccServiceArbiter.Rcc;
using Korone.RccServiceArbiter.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Korone.RccServiceArbiter.Tests;

public sealed class RccProcessPoolTests
{
    [Fact]
    public async Task StopGameServer_ReusesRccWithinConfiguredBoundAndThenKills()
    {
        var fixture = new PoolFixture(maxReuseCount: 2);
        var firstJobId = Guid.NewGuid();
        var secondJobId = Guid.NewGuid();

        var first = await fixture.Pool.StartGameServerAsync(CreateRequest(firstJobId), CancellationToken.None);
        Assert.True(await fixture.Pool.StopGameServerAsync(firstJobId, CancellationToken.None));
        var second = await fixture.Pool.StartGameServerAsync(CreateRequest(secondJobId), CancellationToken.None);
        Assert.True(await fixture.Pool.StopGameServerAsync(secondJobId, CancellationToken.None));

        Assert.Equal(first.RccProcessId, second.RccProcessId);
        Assert.Contains(fixture.Processes, process => process.Id == first.RccProcessId && process.Killed);
    }

    [Fact]
    public async Task CleanUp_KillsExpiredActiveServer()
    {
        var fixture = new PoolFixture(maxReuseCount: 5);
        var jobId = Guid.NewGuid();
        await fixture.Pool.StartGameServerAsync(CreateRequest(jobId), CancellationToken.None);
        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddSeconds(20);

        var removed = await fixture.Pool.CleanUpAsync(CancellationToken.None);

        Assert.Equal(1, removed);
        Assert.Equal(0, fixture.Pool.GetStatistics().ServerCount);
    }

    private static StartGameServerRequest CreateRequest(Guid jobId)
    {
        return new StartGameServerRequest
        {
            JobId = jobId,
            PlaceId = 123,
            UniverseId = 456,
            MaxPlayerCount = 12,
            CreatorId = 789,
            PlaceVersion = 1,
            MatchmakingContextId = 1,
            Year = 2021,
        };
    }

    private sealed class PoolFixture
    {
        public PoolFixture(int maxReuseCount)
        {
            Clock = new MutableClock();
            Ports = new FakePortAllocator();
            Launcher = new FakeProcessLauncher();
            Processes = Launcher.Processes;
            Pool = new RccProcessPool(
                Options.Create(new ArbiterOptions
                {
                    RccServiceRoot = "RCCService",
                    QuilkinPath = "quilkin.exe",
                    Processes = new ArbiterProcessOptions
                    {
                        MaxReuseCount = maxReuseCount,
                        ReservePerYear = 1,
                        IdleTtlSeconds = 300,
                        StartupTimeoutSeconds = 1,
                        JobExpirationSeconds = 10,
                    },
                    Ports = new ArbiterPortOptions(),
                }),
                Ports,
                Launcher,
                new FakeReadinessProbe(),
                new FakeSoapClientFactory(),
                new RccJsonPayloadFactory(Options.Create(new ArbiterOptions())),
                new FakePostStartQueue(),
                Clock,
                NullLogger<RccProcessPool>.Instance);
        }

        public RccProcessPool Pool { get; }
        public MutableClock Clock { get; }
        public FakePortAllocator Ports { get; }
        public FakeProcessLauncher Launcher { get; }
        public List<FakeManagedProcess> Processes { get; }
    }

    private sealed class MutableClock : IArbiterClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakePortAllocator : IPortAllocator
    {
        private readonly Queue<int> _ports = new(new[] { 45000, 50000, 30000, 50001, 30001 });

        public int Allocate(PortRange range) => _ports.Dequeue();
        public void Release(int port) { }
    }

    private sealed class FakeProcessLauncher : IRccProcessLauncher
    {
        private int _nextId = 100;
        public List<FakeManagedProcess> Processes { get; } = new();

        public IManagedProcess Start(string fileName, string arguments, string? workingDirectory = null)
        {
            var process = new FakeManagedProcess(++_nextId);
            Processes.Add(process);
            return process;
        }
    }

    private sealed class FakeManagedProcess : IManagedProcess
    {
        public FakeManagedProcess(int id)
        {
            Id = id;
        }

        public int? Id { get; }
        public bool Killed { get; private set; }
        public bool HasExited => Killed;
        public void KillTree() => Killed = true;
        public void Dispose() { }
    }

    private sealed class FakeReadinessProbe : IRccReadinessProbe
    {
        public Task WaitUntilAvailableAsync(int port, TimeSpan timeout, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSoapClientFactory : IRccSoapClientFactory
    {
        public IRccSoapClient Create(int port) => new FakeSoapClient();
    }

    private sealed class FakeSoapClient : IRccSoapClient
    {
        public Task OpenJobExAsync(Job job, ScriptExecution script, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteExAsync(string jobId, ScriptExecution script, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task CloseJobAsync(string jobId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<RccServiceJob>> GetAllJobsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RccServiceJob>>(Array.Empty<RccServiceJob>());
    }

    private sealed class FakePostStartQueue : IArbiterPostStartQueue
    {
        public ValueTask EnqueueAsync(ArbiterPostStartAction action, CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public async IAsyncEnumerable<ArbiterPostStartAction> ReadAllAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
