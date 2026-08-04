using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace GameServer;

/// <summary>
/// Configuration options for GameServer that can be updated at runtime.
/// </summary>
public sealed record ServerManagerConfig : IValidatableObject
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
    public List<GameProcessManagerConfig> Servers { get; set; } = new();

    public TimeOnly BackupTime { get; set; } = TimeOnly.MinValue;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        List<ValidationResult> results = [];

        foreach (var server in Servers) 
        {
            Validator.TryValidateObject(server, new ValidationContext(server), results, true);
        }

        return results;
    }
}
