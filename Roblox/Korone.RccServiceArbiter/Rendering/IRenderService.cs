using Roblox.Rendering;

namespace Korone.RccServiceArbiter.Rendering;

public interface IRenderService
{
    Task<RenderResult> RenderAsync(RenderRequest request, CancellationToken cancellationToken);
    RenderStatistics GetStatistics();
    int CleanUpIdleWorkers();
}

public sealed class RenderStatistics
{
    public int WorkerCount { get; set; }
    public int IdleWorkerCount { get; set; }
    public int RunningJobs { get; set; }
    public int QueuedJobs { get; set; }
    public long CompletedJobs { get; set; }
    public long FailedJobs { get; set; }
    public int Capacity { get; set; }
    public int QueueCapacity { get; set; }
}

