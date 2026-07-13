using Korone.RccServiceArbiter.Configuration;
using Korone.RccServiceArbiter.Rendering;
using Microsoft.Extensions.Options;

namespace Korone.RccServiceArbiter.Services;

public sealed class RenderWorkerCleanupService(IRenderService renderer, IOptions<ArbiterOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.Processes.CleanupIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken)) renderer.CleanUpIdleWorkers();
    }
}
