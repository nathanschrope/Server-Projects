using System.Diagnostics;

namespace HealthAPI
{
    public class ApplicationChecker
    {
        private readonly ILogger<ApplicationChecker> _logger;

        public ApplicationChecker(ILogger<ApplicationChecker> logger)
        {
            _logger = logger;
        }

        public bool IsApplicationRunning(string applicationName)
        {
            return Process.GetProcessesByName(applicationName).Length > 0;
        }
    }
}
