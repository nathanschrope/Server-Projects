using System.Text.Json;

namespace DiscordWorker;

internal class HealthChecker : IHealthChecker
{
    private bool isServerDown = false;
    private HealthResponse? _serverStatus { get; set; } = null;

    public async Task<List<string>> GetHealthAsync(CancellationToken cancellationToken)
    {
        List<string> messages = [];
        HttpClient client = new();

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        var result = await client.GetAsync("http://localhost:8069/server/health", cts.Token);
        if (result.IsSuccessStatusCode)
        {
            string responsestr = await result.Content.ReadAsStringAsync(cts.Token);
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
        client.Dispose();

        return messages;
    }
}
