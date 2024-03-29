using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HealthAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : Controller
    {
        private readonly ILogger<HealthController> _logger;

        private JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        private string[] _applicationNames = new string[]
        {
            "enshrouded_server",
            "SonsOfTheForestDS",
            "valheim_server"
        };

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("")]
        public string Get()
        {
            HealthResponse response = new HealthResponse();
            ApplicationChecker applicationChecker = new ApplicationChecker();

            foreach (var app in _applicationNames)
            {
                if (applicationChecker.IsApplicationRunning(app))
                    response.statusList.Add(new ApplicationStatus() { Name = app, Status = "healthy"});
                else
                    response.statusList.Add(new ApplicationStatus() { Name = app, Status = "down" });
            }

            return JsonSerializer.Serialize(response, _serializerOptions);
        }
    }
}
