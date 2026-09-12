using System.Text.Json;

namespace DiscordWorker;

internal class HealthChecker(ILogger<HealthChecker> logger) : IHealthChecker
{
    private bool isServerDown = false;
    private HealthResponse? _serverStatus { get; set; } = null;

    public async Task<List<string>> GetHealthAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking server health...");
        List<string> messages = [];
        HttpClient client = new();

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        HttpResponseMessage? result = null;
        try
        {
            result = await client.GetAsync("http://localhost:8069/server/health", cts.Token);
        } 
        catch (OperationCanceledException)
        { }

        if (result?.IsSuccessStatusCode is true)
        {
            string? responsestr = null;
            try
            {
                responsestr = await result.Content.ReadAsStringAsync(cts.Token);
            }
            catch(OperationCanceledException)
            { }

            if (responsestr is not null)
            {
                var jsonObject = JsonSerializer.Deserialize<HealthResponse>(responsestr);

                if (jsonObject != null)
                {
                    if (isServerDown)
                    {
                        isServerDown = false;
                        messages.Add("server is back up");
                    }

                    if (_serverStatus == null)
                    {
                        _serverStatus = jsonObject;
                    }
                    else
                    {
                        var differences = jsonObject.StatusList.Except(_serverStatus.StatusList, new ApplicationStatusComparer());
                        foreach (var dif in differences)
                        {
                            messages.Add($"{dif.Name} is {dif.Status} ({dif.NumberOfProcesses})");
                        }

                        _serverStatus.StatusList = jsonObject.StatusList;
                    }
                }
                else
                {
                    if (!isServerDown)
                    {
                        isServerDown = true;
                        messages.Add("server is down");
                    }
                }
            }
            else
            {
                if (!isServerDown)
                {
                    isServerDown = true;
                    messages.Add("server is down");
                }
            }
        }
        else
        {
            if (!isServerDown)
            {
                isServerDown = true;
                messages.Add("server is down");
            }
        }
        client.Dispose();

        return messages;
    }
}