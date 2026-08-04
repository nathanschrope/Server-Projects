
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GameServer.Controllers;

[Route("Server")]
[ApiController]
public class HealthController(IServerManager serverManager, IOptionsMonitor<ServerManagerConfig> configMonitor) : ControllerBase
{
    private JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    [HttpGet]
    [Route("health")]
    public async Task<string> GetAsync()
    {
        var servers = await serverManager.GetRunningServersAsync();

        HealthResponse response = new HealthResponse();

        foreach (string server in servers)
        {
            response.StatusList.Add(new ApplicationStatus() { Name = server, Status = "healthy" });
        }
        var config = configMonitor.CurrentValue;

        foreach (var server in config.Servers)
        {
            var found = servers.Where(s => s.Equals(server.Name, StringComparison.OrdinalIgnoreCase)).Any();
            if (!found)
            {
                response.StatusList.Add(new ApplicationStatus() { Name = server.Name, Status = "down" });
            }
        }

        return JsonSerializer.Serialize(response, _serializerOptions);
    }
}