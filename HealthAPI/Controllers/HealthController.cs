using CommonLibrary;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HealthAPI.Controllers
{
    [ApiController]
    [Route("Server")]
    public class HealthController : Controller
    {
        private readonly ILogger<HealthController> _logger;
        private List<Service> _services;
        private JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
            _services =
            [
                new Service("enshrouded_server"),
                new Service("SonsOfTheForestDS"),
                new Service("valheim_server"),
                new Service("minecraft") { CheckTitle = "java" },
            ];
        }

        [HttpGet]
        [Route("health")]
        public string Get()
        {
            HealthResponse response = new HealthResponse();
            ApplicationChecker applicationChecker = new ApplicationChecker();

            foreach (var app in _services)
            {
                _logger.LogInformation($"Finding {app.Name} {app.CheckTitle} to check health");
                if (applicationChecker.IsApplicationRunningByName(app.Name))
                    response.StatusList.Add(new ApplicationStatus() { Name = app.Name, Status = "healthy"});
                else if(!String.IsNullOrEmpty(app.CheckTitle) && applicationChecker.IsApplicationRunningByTitle(app.CheckTitle, app.Name))
                    response.StatusList.Add(new ApplicationStatus() { Name = app.Name, Status = "healthy" });
                else
                    response.StatusList.Add(new ApplicationStatus() { Name = app.Name, Status = "down" });
            }

            return JsonSerializer.Serialize(response, _serializerOptions);
        }
    }
}
