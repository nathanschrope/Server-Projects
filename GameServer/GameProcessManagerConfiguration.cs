using System.ComponentModel.DataAnnotations;

namespace GameServer;

/// <summary>
/// Configuration options for GameServer that can be updated at runtime.
/// </summary>
public sealed record GameProcessManagerConfiguration
{
    public const string SectionName = "GameServer";

    /// <summary>
    /// Interval (in milliseconds) at which the worker checks server health.
    /// </summary>
    public int WorkerIntervalMs { get; set; } = 5000;

    /// <summary>
    /// List of game server instances to manage.
    /// </summary>
    [MinLength(1)]
    public List<ServerInstance> Servers { get; set; } = new();

    [Required]
    public required TimeOnly BackupTime { get; set; }
}
