using System.Text.Json;

namespace GameServer.Discord;

internal class HealthChecker : IHealthChecker
{
    private HealthResponse? _serverStatus { get; set; } = null;

    public async Task<List<string>> GetHealthAsync(CancellationToken cancellationToken)
    {
        List<string> messages = [];
        HttpClient client = new();

        var result = await client.GetAsync("http://localhost:8069/server/health", cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            string responsestr = await result.Content.ReadAsStringAsync();
            var jsonObject = JsonSerializer.Deserialize<HealthResponse>(responsestr);

            if (jsonObject != null)
            {
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
                messages.Add("server is down");
            }
        }
        else
        {
            messages.Add("server is down");
        }
        client.Dispose();

        return messages;
    }
}
