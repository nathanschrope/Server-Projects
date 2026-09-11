using System.Diagnostics;
using System.IO.Compression;
using System.Management;
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
        lock (_lockObject)
        {
            if (IsRunning)
            {
                _logger.LogInformation("[{serverName}] Server is already running (PID: {pid})", ServerName, _serverProcess?.Id);
                return true;
            }

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

            processToStop = _serverProcess;
        }

        try
        {
            _logger.LogInformation("[{serverName}] Stopping server (PID: {pid})", ServerName, processToStop.Id);

            bool stopped = false;

            // Try writing "stop" to stdin first (works better when running as a service)
            try
            {
                _logger.LogDebug("[{serverName}] Trying stdin 'stop' command", ServerName);
                if (processToStop.StandardInput.BaseStream.CanWrite)
                {
                    processToStop.StandardInput.WriteLine("stop");
                    processToStop.StandardInput.Flush();

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        try
                        {
                            await processToStop.WaitForExitAsync(cts.Token);
                            _logger.LogInformation("[{serverName}] Server stopped via stdin 'stop'", ServerName);
                            stopped = true;
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogWarning("[{serverName}] Server did not stop within 5000ms after 'stop' command", ServerName);
                        }
                    }
                }
            }
            catch
            {
                _logger.LogWarning("[{serverName}] stdin 'stop' failed, will try Ctrl+C", ServerName);
            }

            // If still not stopped, wait for the full timeout before killing
            if (!stopped)
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_config.ShutdownTimeoutMs)))
                {
                    try
                    {
                        _logger.LogDebug("[{serverName}] Waiting for graceful shutdown timeout {timeout}ms", ServerName, _config.ShutdownTimeoutMs);
                        await processToStop.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("[{serverName}] Server did not stop gracefully within {timeout}ms, checking for child processes",
                            ServerName,
                            _config.ShutdownTimeoutMs);

                        // Get child processes before killing parent
                        var childProcesses = GetChildProcesses(processToStop.Id);

                        if (childProcesses.Count > 0)
                        {
                            _logger.LogInformation("[{serverName}] Found {count} child process(es), attempting graceful shutdown", ServerName, childProcesses.Count);
                            await ShutdownChildProcessesGracefully(childProcesses);
                        }

                        // Kill the parent if still running
                        if (!processToStop.HasExited)
                        {
                            _logger.LogDebug("[{serverName}] Killing parent process (PID: {pid})", ServerName, processToStop.Id);
                            processToStop.Kill();
                            processToStop.WaitForExit(5000);
                        }
                    }
                }
            }

            _logger.LogInformation("[{serverName}] Server stopped", ServerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{serverName}] Error stopping server", ServerName);
        }
        finally
        {
            lock (_lockObject)
            {
                if (_serverProcess == processToStop)
                {
                    _serverProcess?.Dispose();
                    _serverProcess = null;
                }
            }
        }
    }

    /// <summary>
    /// Gets all child processes of a given parent process.
    /// </summary>
    private List<Process> GetChildProcesses(int parentPid)
    {
        var childProcesses = new List<Process>();
        try
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    var parentId = GetParentProcessId(proc);
                    if (parentId == parentPid)
                    {
                        childProcesses.Add(proc);
                    }
                }
                catch
                {
                    // Ignore access errors
                }
            }
        }
        catch
        {
            // Ignore errors getting process list
        }
        return childProcesses;
    }

    /// <summary>
    /// Attempts graceful shutdown of child processes.
    /// </summary>
    private async Task ShutdownChildProcessesGracefully(List<Process> childProcesses)
    {
        var shutdownTasks = new List<Task>();

        foreach (var child in childProcesses)
        {
            shutdownTasks.Add(ShutdownSingleProcessGracefully(child));
        }

        try
        {
            // Give children their own timeout to shut down gracefully
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                await Task.WhenAny(
                    Task.WhenAll(shutdownTasks),
                    Task.Delay(-1, cts.Token)
                ).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached, force kill any remaining children
            foreach (var child in childProcesses)
            {
                if (!child.HasExited)
                {
                    _logger.LogWarning("[{serverName}] Child process (PID: {pid}) did not stop within timeout, force killing",
                        ServerName, child.Id);
                    try
                    {
                        child.Kill();
                        child.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[{serverName}] Error force killing child process (PID: {pid})", ServerName, child.Id);
                    }
                }
            }
        }
        finally
        {
            // Cleanup
            foreach (var child in childProcesses)
            {
                child.Dispose();
            }
        }
    }

    /// <summary>
    /// Attempts graceful shutdown of a single process.
    /// </summary>
    private async Task ShutdownSingleProcessGracefully(Process process)
    {
        try
        {
            _logger.LogDebug("[{serverName}] Attempting graceful shutdown of child process (PID: {pid})", ServerName, process.Id);

            if (process.HasExited)
                return;

            // Try stdin first
            try
            {
                if (process.StandardInput.BaseStream.CanWrite)
                {
                    process.StandardInput.WriteLine("stop");
                    process.StandardInput.Flush();

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                    {
                        try
                        {
                            await process.WaitForExitAsync(cts.Token);
                            _logger.LogInformation("[{serverName}] Child process (PID: {pid}) stopped via stdin", ServerName, process.Id);
                            return;
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogDebug("[{serverName}] Child process (PID: {pid}) did not respond to stdin", ServerName, process.Id);
                        }
                    }
                }
            }
            catch
            {
                // stdin not available
            }

            // Try Ctrl+C if running on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    if (NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_C_EVENT, (uint)process.Id))
                    {
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                        {
                            try
                            {
                                await process.WaitForExitAsync(cts.Token);
                                _logger.LogInformation("[{serverName}] Child process (PID: {pid}) stopped via Ctrl+C", ServerName, process.Id);
                                return;
                            }
                            catch (OperationCanceledException)
                            {
                                _logger.LogDebug("[{serverName}] Child process (PID: {pid}) did not respond to Ctrl+C", ServerName, process.Id);
                            }
                        }
                    }
                }
                catch
                {
                    // Ctrl+C failed
                }
            }

            // Wait for natural exit
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                    _logger.LogInformation("[{serverName}] Child process (PID: {pid}) exited", ServerName, process.Id);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("[{serverName}] Child process (PID: {pid}) still running after all shutdown attempts", ServerName, process.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{serverName}] Error shutting down child process (PID: {pid})", ServerName, process.Id);
        }
    }

    /// <summary>
    /// Gets the parent process ID using WMI.
    /// </summary>
    private int GetParentProcessId(Process process)
    {
        try
        {
            var query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {process.Id}";
            using (var searcher = new System.Management.ManagementObjectSearcher(query))
            {
                var results = searcher.Get();
                foreach (var obj in results)
                {
                    return Convert.ToInt32(obj["ParentProcessId"]);
                }
            }
        }
        catch
        {
            // If WMI fails, return -1
        }
        return -1;
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
