using Microsoft.Extensions.Options;

namespace GameServer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptionsMonitor<GameProcessManagerConfiguration> _optionsMonitor;
    private readonly Dictionary<string, GameProcessManager> _serverManagers = new();

    private DateTime nextBackupTime;

    public Worker(
        ILogger<Worker> logger,
        ILoggerFactory loggerFactory,
        IOptionsMonitor<GameProcessManagerConfiguration> optionsMonitor)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _optionsMonitor = optionsMonitor;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GameServer Worker starting - managing {count} server(s)", _optionsMonitor.CurrentValue.Servers.Count);

        // Initialize managers for all configured servers
        foreach (var serverConfig in _optionsMonitor.CurrentValue.Servers)
        {
            if (!string.IsNullOrWhiteSpace(serverConfig.Name))
            {
                var processLogger = _loggerFactory.CreateLogger<GameProcessManager>();
                _serverManagers[serverConfig.Name] = new GameProcessManager(processLogger, serverConfig);
            }
        }

        // Start all enabled servers
        foreach (var (name, manager) in _serverManagers)
        {
            var serverConfig = _optionsMonitor.CurrentValue.Servers.FirstOrDefault(s => s.Name == name);
            if (serverConfig?.Enabled ?? false)
            {
                await manager.StartServerAsync();
            }
        }

        DateTime now = DateTime.Now;
        if (TimeOnly.FromDateTime(now) > _optionsMonitor.CurrentValue.BackupTime)
        {
            nextBackupTime = now.Date.AddDays(1) + _optionsMonitor.CurrentValue.BackupTime.ToTimeSpan();
        }
        else
        {
            nextBackupTime = now.Date + _optionsMonitor.CurrentValue.BackupTime.ToTimeSpan();
        }

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GameServer Worker stopping - shutting down all servers");

        // Stop all servers gracefully
        IEnumerable<Task> stopTasks = _serverManagers.Values.Select(m => m.StopServerAsync());
        await Task.WhenAll(stopTasks);

        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var config = _optionsMonitor.CurrentValue;

                if (DateTime.Now > nextBackupTime)
                {
                    List<Task> tasks = [];

                    foreach (var serverMan in _serverManagers.Values)
                    {
                        tasks.Add(serverMan.BackupAsync());
                    }

                    await Task.WhenAll(tasks);

                    nextBackupTime = DateTime.Now.Date + config.BackupTime.ToTimeSpan();
                }
                else
                {
                    List<Task> serverCheckTasks = [];

                    foreach (var serverConfig in config.Servers)
                    {
                        if (string.IsNullOrWhiteSpace(serverConfig.Name))
                            continue;

                        // Create a task for this server check and add to list
                        serverCheckTasks.Add(CheckAndManageServerAsync(serverConfig, stoppingToken));
                    }

                    // Wait for all server checks to complete in parallel
                    await Task.WhenAll(serverCheckTasks);

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        var status = string.Join(" | ", _serverManagers.Select(
                            kv => $"{kv.Key}:{(kv.Value.IsRunning ? "Running" : "Stopped")}"));
                        _logger.LogDebug("Health check complete - {status}", status);
                    }
                }
               
                await Task.Delay(config.WorkerIntervalMs, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker loop");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task CheckAndManageServerAsync(ServerInstance serverConfig, CancellationToken cancellationToken)
    {
        if (!_serverManagers.TryGetValue(serverConfig.Name, out var manager))
        {
            // Create manager if it doesn't exist (config was added)
            var processLogger = _loggerFactory.CreateLogger<GameProcessManager>();
            manager = new GameProcessManager(processLogger, serverConfig);
            _serverManagers[serverConfig.Name] = manager;
        }

        if (serverConfig.Enabled)
        {
            bool isHealthy = manager.CheckHealth();

            if (!isHealthy && serverConfig.AutoRestartOnCrash)
            {
                _logger.LogWarning("Server '{serverName}' is not running, attempting restart...", serverConfig.Name);
                await manager.StartServerAsync();
            }
            else if (!isHealthy)
            {
                _logger.LogWarning("Server '{serverName}' crashed but auto-restart is disabled", serverConfig.Name);
            }
        }
        else if (manager.IsRunning)
        {
            // Server was disabled in config, stop it
            _logger.LogInformation("Server '{serverName}' disabled in config, stopping...", serverConfig.Name);
            await manager.StopServerAsync();
        }
    }
}
