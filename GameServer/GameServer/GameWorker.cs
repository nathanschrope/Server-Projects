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
        logger.LogInformation("GameServer Worker stopping - shutting down all servers");

        // First stop all servers gracefully so they receive shutdown while the worker is still running.
        logger.LogInformation("Calling serverManager.StopAllAsync to stop all servers");
        try
        {
            await serverManager.StopAllAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping servers during shutdown");
        }


        // Then signal the background loop to stop so it doesn't restart servers while we're shutting down.
        logger.LogInformation("Calling base.StopAsync to signal background loop");
        await base.StopAsync(cancellationToken);

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