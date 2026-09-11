using Microsoft.Extensions.Options;

namespace GameServer.GameServer;

public class GameWorker(ILogger<GameWorker> logger, IServerManager serverManager, IOptionsMonitor<ServerManagerConfig> optionsMonitor) : BackgroundService
{
    private DateTime nextBackupTime;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("GameServer Worker starting");

        await serverManager.StartAllAsync();

        nextBackupTime = DateTime.Now.Date.AddDays(1) + optionsMonitor.CurrentValue.BackupTime.ToTimeSpan();

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("GameServer Worker stopping - signaling background loop to stop first");

        // First, stop the background loop so it doesn't restart servers while we're shutting down
        await base.StopAsync(cancellationToken);

        logger.LogInformation("Background loop stopped, now shutting down all servers");

        try
        {
            await serverManager.StopAllAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping servers during shutdown");
        }

        logger.LogInformation("GameServer Worker stopped");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.Now > nextBackupTime)
                {
                    await serverManager.TriggerBackupAsync();

                    nextBackupTime = DateTime.Now.Date.AddDays(1) + optionsMonitor.CurrentValue.BackupTime.ToTimeSpan();
                }
                else
                {
                    await serverManager.StartAllAsync();
                }
               
                await Task.Delay(optionsMonitor.CurrentValue.WorkerIntervalMs, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in worker loop");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}