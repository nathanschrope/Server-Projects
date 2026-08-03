using System.ComponentModel.DataAnnotations;

namespace GameServer;

/// <summary>
/// Configuration for a single game server instance.
/// </summary>
public sealed record ServerInstance
{
    /// <summary>
    /// Name/identifier for this server.
    /// </summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    /// Whether this server is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Path to the executable or script to run (e.g., java, python, ./game-server.exe).
    /// </summary>
    [Required]
    public required string FileName { get; set; }

    /// <summary>
    /// Command line arguments to pass to the executable (includes any memory settings).
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// Working directory for the server process.
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Whether to automatically restart this server if it crashes.
    /// </summary>
    public bool AutoRestartOnCrash { get; set; } = true;

    /// <summary>
    /// Timeout in milliseconds for graceful shutdown before killing the process.
    /// </summary>
    public int ShutdownTimeoutMs { get; set; } = 30000;

    public List<BackupLocation> BackupLocations { get; set; } = [];

    public string UpdateScript { get; set; } = string.Empty;
}

public sealed record BackupLocation
{
    [Required]
    public required string Name { get; set; }
    [Required]
    public required string BackupFromDirectory { get; set; }

    [Required]
    public required string BackupToDirectory { get; set; }
}