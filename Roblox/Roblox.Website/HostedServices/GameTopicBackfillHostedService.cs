using Roblox.Services.Games;

namespace Roblox.Website.HostedServices;

public class GameTopicBackfillHostedService : BackgroundService
{
    private readonly ILogger<GameTopicBackfillHostedService> _logger;

    public GameTopicBackfillHostedService(ILogger<GameTopicBackfillHostedService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(GameTopicService.BackfillStartupDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using var timer = new PeriodicTimer(GameTopicService.BackfillIntervalTime);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GameTopicService.RunBackfillCycleAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Topic backfill cycle failed.");
                Console.WriteLine("[warn] topic backfill cycle failed: {0}", e.Message);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
