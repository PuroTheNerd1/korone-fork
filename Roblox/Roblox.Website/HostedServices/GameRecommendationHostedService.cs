using Roblox.Services.Games;

namespace Roblox.Website.HostedServices;

public class GameRecommendationHostedService : BackgroundService
{
    private readonly ILogger<GameRecommendationHostedService> _logger;

    public GameRecommendationHostedService(ILogger<GameRecommendationHostedService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(GameRecommendationService.StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using var timer = new PeriodicTimer(GameRecommendationService.RefreshInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GameRecommendationService.RunPeriodicCycleAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Recommendation cron cycle failed.");
                Console.WriteLine("[warn] recommendation cron cycle failed: {0}", e.Message);
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
