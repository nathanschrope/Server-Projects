using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace GameServer.GameServer;

/// <summary>
/// P/Invoke declarations for Windows console control.
/// </summary>
internal static class NativeMethods
{
    public const uint CREATE_NEW_PROCESS_GROUP = 0x00000200;
    public const uint CTRL_C_EVENT = 0;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
}

/// <summary>
/// Manages a generic game server process - starting, monitoring, and restarting.
/// </summary>
public class GameProcessManager
{
    private readonly ILogger _logger;
    private readonly GameProcessManagerConfig _config;
    private Process? _serverProcess;
    private readonly object _lockObject = new();
    private volatile bool _runningStart = false;
    private const string DATETIME_PATTERN = "yyyyMMdd";

    public string ServerName => _config.Name;
    public bool IsRunning => _serverProcess != null && !_serverProcess.HasExited;

    public GameProcessManager(ILogger logger, GameProcessManagerConfig config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Starts the game server if it's not already running.
    /// </summary>
    public async Task<bool> StartServerAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.FileName))
        {
            _logger.LogError("[{serverName}] FileName not configured", ServerName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_config.WorkingDirectory))
        {
            _logger.LogError("[{serverName}] WorkingDirectory not configured", ServerName);
            return false;
        }

        if (!Directory.Exists(_config.WorkingDirectory))
        {
            _logger.LogError("[{serverName}] Working directory does not exist: {path}", ServerName, _config.WorkingDirectory);
            return false;
        }

        if (_runningStart)
        {
            return true;
        }

        lock (_lockObject)
        {
            _runningStart = true;

            if (IsRunning)
            {
                _logger.LogInformation("[{serverName}] Server is already running (PID: {pid})", ServerName, _serverProcess?.Id);
                return true;
            }

            if (!string.IsNullOrEmpty(_config.UpdateScript))
            {
                var updateInfo = new ProcessStartInfo
                {
                    FileName = _config.UpdateScript,
                    Arguments = string.Empty,
                    WorkingDirectory = string.Empty,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                };

                try
                {
                    Process.Start(updateInfo)?.WaitForExit();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{serverName}] Error running update script: {script}", ServerName, _config.UpdateScript);
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _config.FileName,
                Arguments = _config.Arguments,
                WorkingDirectory = _config.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            try
            {
                _serverProcess = Process.Start(startInfo);

                if (_serverProcess == null)
                {
                    _logger.LogError("[{serverName}] Failed to start server", ServerName);
                    return false;
                }

                _logger.LogInformation("[{serverName}] Server started (PID: {pid}) using {fileName} {arguments}",
                    ServerName,
                    _serverProcess.Id,
                    _config.FileName,
                    _config.Arguments);

                // Capture output asynchronously
                _ = Task.Run(() => CaptureOutput(_serverProcess.StandardOutput, "OUT"));
                _ = Task.Run(() => CaptureOutput(_serverProcess.StandardError, "ERR"));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{serverName}] Error starting server", ServerName);
                return false;
            }
            finally
            {
                _runningStart = false;
            }
        }
    }

    /// <summary>
    /// Stops the server gracefully.
    /// </summary>
    public async Task StopServerAsync()
    {
        Process? processToStop = null;

        lock (_lockObject)
        {
            if (_serverProcess == null || _serverProcess.HasExited)
            {
                _logger.LogDebug("[{serverName}] Server is not running", ServerName);
                return;
            }

            _runningStart = true; // hopefully prevents startup being called after stop
            processToStop = _serverProcess;
        }

        try
        {
            _logger.LogInformation("[{serverName}] Stopping server (PID: {pid})", ServerName, processToStop.Id);

            try
            {
                // this only works for minecraft right now
                if (processToStop.StandardInput.BaseStream.CanWrite)
                {
                    processToStop.StandardInput.WriteLine("stop");
                    processToStop.StandardInput.Flush();

                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_config.ShutdownTimeoutMs));
                    await processToStop.WaitForExitAsync(cts.Token);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{serverName}] Error sending stop command to server (PID: {pid})", ServerName, processToStop.Id);
            }

            try
            {
                processToStop.CloseMainWindow();
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_config.ShutdownTimeoutMs));
                await processToStop.WaitForExitAsync(cts.Token);
                return;
            }
            catch (Exception)
            {
                _logger.LogWarning("[{serverName}] Server did not exit in time, force killing (PID: {pid})", ServerName, processToStop.Id);
                processToStop.Kill(true);
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_config.ShutdownTimeoutMs));
                await processToStop.WaitForExitAsync(cts.Token);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{serverName}] Error stopping server", ServerName);
        }
        finally
        {
            lock (_lockObject)
            {
                _runningStart = false;
                if (_serverProcess == processToStop)
                {
                    _serverProcess?.Dispose();
                    _serverProcess = null;
                }
            }
        }
    }

    /// <summary>
    /// Checks if server is still running.
    /// </summary>
    public bool CheckHealth()
    {
        lock (_lockObject)
        {
            return IsRunning;
        }
    }

    public async Task BackupAsync()
    {
        if (_config.BackupLocations.Count > 0)
        {
            await StopServerAsync();

            foreach (var backupLocation in _config.BackupLocations)
            {
                Backup(backupLocation.BackupFromDirectory, backupLocation.BackupToDirectory, "Backup_" + backupLocation.Name + "_" + DateTime.Now.ToString(DATETIME_PATTERN) + ".zip");
            }

            await StartServerAsync();

            Cleanup();
        }
    }

    private void Backup(string backupFromDirectory, string backupToDirectory, string filename)
    {
        if (!Directory.Exists(backupFromDirectory))
        {
            _logger.LogCritical("Backup source directory does not exist: {backupFromDirectory}", backupFromDirectory);
            return;
        }

        if (!Directory.Exists(backupToDirectory))
        {
            Directory.CreateDirectory(backupToDirectory);
        }

        string fullBackupPath = Path.Combine(backupToDirectory, filename);

        if (File.Exists(fullBackupPath))
        {
            _logger.LogCritical("Backup file already exists: {backupFile}", fullBackupPath);
            return;
        }

        _logger.LogInformation($"Getting Backup of {backupFromDirectory} to {backupToDirectory}");

        try
        {
            ZipFile.CreateFromDirectory(backupFromDirectory, fullBackupPath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"ZIP FAILED {backupFromDirectory}");
        }
    }

    private void Cleanup()
    {
        foreach (var backupLocation in _config.BackupLocations)
        {
            var backupDirectory = backupLocation.BackupToDirectory;
            if (Directory.Exists(backupDirectory))
            {
                var backupFiles = Directory.GetFiles(backupDirectory, "Backup_" + backupLocation.Name + "_*.zip");
                Array.Sort(backupFiles);
                for (int i = 0; i < backupFiles.Length - _config.MaxBackupCount; i++)
                {
                    File.Delete(backupFiles[i]);
                }
            }
        }
    }

    private async Task CaptureOutput(StreamReader reader, string source)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                _logger.LogInformation("[{serverName}:{source}] {output}", ServerName, source, line);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{serverName}] Error capturing {source} stream", ServerName, source);
        }
    }
}
