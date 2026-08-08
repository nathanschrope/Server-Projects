using Microsoft.Extensions.Options;

namespace GameServer.GameServer;


public interface IServerManager
{
    Task StartServerAsync(string serverName);
    Task StartAllAsync();
    Task StopServerAsync(string serverName);
    Task StopAllAsync();
    Task RestartServerAsync(string serverName);
    Task RestartAllAsync();
    Task<bool> IsServerRunningAsync(string serverName);
    Task<List<string>> GetRunningServersAsync();
    Task TriggerBackupAsync();
}

public class ServerManager(ILogger<ServerManager> logger, ILoggerFactory loggerFactory, IOptionsMonitor<ServerManagerConfig> optionsMonitor) : IServerManager
{
    private const int DELAY_BETWEEN_RESTARTS_MS = 1000;
    private readonly Dictionary<string, GameProcessManager> _serverManagers = new(StringComparer.OrdinalIgnoreCase);

    public Task StartServerAsync(string serverName) 
    {
        var config = optionsMonitor.CurrentValue;

        return StartServerAsync(config, serverName);
    }

    public Task StartAllAsync()
    {
        var config = optionsMonitor.CurrentValue;

        List<Task> tasks = [];
        foreach (var server in config.Servers)
        {
            tasks.Add(StartServerAsync(config, server.Name));
        }

        return Task.WhenAll(tasks);
    }

    private async Task StartServerAsync(ServerManagerConfig config, string serverName)
    {
        // checking if server exists already
        if (_serverManagers.TryGetValue(serverName, out var manager))
        {
            // IT DOES, If its running just continue
            if (manager.IsRunning)
                return;

            // its not running, start it
            await manager.StartServerAsync();
            return;
        }
        else
        {
            // server does not exist

            // check if the server is in the config
            var serverConfig = config.Servers.FirstOrDefault(x => x.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));
            if (serverConfig is null)
            {
                // config missing, report and continue
                logger.LogWarning("Server not found in configuration: {ServerName}", serverName);
                return;
            }

            // server is in config, lets start it up
            var newManager = new GameProcessManager(loggerFactory.CreateLogger(serverConfig.Name), serverConfig);
            _serverManagers[serverName] = newManager;
            await newManager.StartServerAsync();
            return;
        }
    }

    public Task StopServerAsync(string serverName)
    {
        if (_serverManagers.TryGetValue(serverName, out var manager))
        {
            return manager.StopServerAsync();
        }

        logger.LogWarning("Server asked to stop but was not found: {ServerName}", serverName);
        return Task.CompletedTask;
    }

    public Task StopAllAsync()
    {
        List<Task> tasks = [];
        foreach (var manager in _serverManagers.Values)
        {
            tasks.Add(manager.StopServerAsync());
        }
        return Task.WhenAll(tasks);
    }

    public Task RestartServerAsync(string serverName)
    {
        return StopServerAsync(serverName)
            .ContinueWith(_ => Task.Delay(DELAY_BETWEEN_RESTARTS_MS))
            .ContinueWith(_ => StartServerAsync(serverName));
    }

    public Task RestartAllAsync()
    {
        List<Task> tasks = [];
        foreach (var manager in _serverManagers)
        {
            tasks.Add(RestartServerAsync(manager.Key));
        }

        return Task.WhenAll(tasks);
    }

    public Task<bool> IsServerRunningAsync(string serverName)
    {
        if (_serverManagers.TryGetValue(serverName, out var manager))
        {
            return Task.FromResult(manager.IsRunning);
        }

        return Task.FromResult(false);
    }

    public Task<List<string>> GetRunningServersAsync()
    {
        List<string> runningServers = new();
        foreach (var kvp in _serverManagers)
        {
            if (kvp.Value.IsRunning)
            {
                runningServers.Add(kvp.Key);
            }
        }
        return Task.FromResult(runningServers);
    }

    public Task TriggerBackupAsync()
    {
        List<Task> tasks = [];
        foreach (var manager in _serverManagers.Values)
        {
            tasks.Add(manager.BackupAsync());
        }
        return Task.WhenAll(tasks);
    }
}
