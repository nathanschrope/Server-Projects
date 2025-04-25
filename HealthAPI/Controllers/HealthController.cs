using CommonLibrary;
using CommonLibrary.StartStop;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace HealthAPI.Controllers
{
    [ApiController]
    [Route("Server")]
    public class HealthController : Controller
    {
        private readonly ILogger<HealthController> _logger;
        private IConfig<IApplication> _config;
        private JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public HealthController(ILogger<HealthController> logger, IConfig<IApplication> config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        [Route("health")]
        public string Get()
        {
            HealthResponse response = new HealthResponse();

            foreach (var app in _config.Applications)
            {
                _logger.LogInformation($"Finding {app.Name} {app.CheckTitle} to check health");
                int foundApps = Process.GetProcessesByName(app.Name).Length;
                if (!String.IsNullOrEmpty(app.CheckTitle) && foundApps == 0)
                    foundApps = Process.GetProcessesByName(app.CheckTitle).Length;

                if (foundApps > 0)
                    response.StatusList.Add(new ApplicationStatus() { Name = app.Name, Status = "healthy", NumberOfProcesses = foundApps });
                else
                    response.StatusList.Add(new ApplicationStatus() { Name = app.Name, Status = "down" });
            }

            return JsonSerializer.Serialize(response, _serializerOptions);
        }
    }
}