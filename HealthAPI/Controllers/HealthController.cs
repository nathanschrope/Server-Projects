using Microsoft.AspNetCore.Mvc;

namespace HealthAPI.Controllers
{
    [ApiController]
    public class HealthController : Controller
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "health")]
        public string Get()
        {
            return "healthy";
        }
    }
}
