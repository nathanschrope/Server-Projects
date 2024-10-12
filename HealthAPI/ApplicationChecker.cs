using CommonLibrary;
using System.Diagnostics;

namespace HealthAPI
{
    public class ApplicationChecker
    {

        public ApplicationChecker()
        {

        }

        public int IsApplicationRunningByName(string applicationName)
        {
            return Process.GetProcessesByName(applicationName).Length;
        }

        public int IsApplicationRunningByTitle(string applicationName, string title)
        {
            var processes = Process.GetProcessesByName(applicationName);
            if (processes.Length > 0)
                processes = processes.Where(x => x.MainWindowTitle.Equals(title, StringComparison.OrdinalIgnoreCase)).ToArray();
            return processes.Length;
        }
    }
}
