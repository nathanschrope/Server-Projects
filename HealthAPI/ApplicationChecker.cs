using System.Diagnostics;

namespace HealthAPI
{
    public class ApplicationChecker
    {

        public ApplicationChecker()
        {

        }

        public bool IsApplicationRunning(string applicationName)
        {
            return Process.GetProcessesByName(applicationName).Length > 0;
        }
    }
}
