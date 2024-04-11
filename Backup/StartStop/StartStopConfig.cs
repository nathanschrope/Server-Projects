using CommonLibrary;

namespace Backup.StartStop
{
    public class StartStopConfig
    {
        public string StartUpFolder { get; set; } = "%AppData%\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\";

        public List<Service> Services { get; set; } = new List<Service>();
    }
}
