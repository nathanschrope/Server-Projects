using Microsoft.Extensions.Options;

namespace GameServer;

public class Worker(ILogger<Worker> logger, IServerManager serverManager, IOptionsMonitor<ServerManagerConfig> optionsMonitor) : BackgroundService
{
    private DateTime nextBackupTime;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("GameServer Worker starting");

        await serverManager.StartAllAsync();

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("GameServer Worker stopping - shutting down all servers");

        // Stop all servers gracefully
        await serverManager.StopAllAsync();

        await base.StopAsync(cancellationToken);
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