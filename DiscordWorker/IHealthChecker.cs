namespace DiscordWorker;

internal interface IHealthChecker
{
    public Task<List<string>> GetHealthAsync(CancellationToken cancellationToken);
}
