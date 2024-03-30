using System.Diagnostics;

namespace Backup
{
    public class StartAndStopService
    {
        private readonly ILogger _logger;
        private List<string> _services;
        private Dictionary<string, bool> _running;
        private readonly string _startUpFolder = "C:\\Users\\admin\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\";
        public StartAndStopService(ILogger<StartAndStopService> logger) 
        { 
            _logger = logger;
            _services = new();
            _running = new();
            Configure();
        }

        public void Configure()
        {
            _services.Add("enshrouded_server");
            _services.Add("SonsOfTheForestDS");
            _services.Add("valheim_server");
        }

        public async Task StopServicesAsync()
        {
            var tasks = new List<Task>();
            foreach (var service in _services)
            {
                try
                {
                    var processes = Process.GetProcessesByName(service);
                    _logger.LogInformation($"{service} found {processes.Length} services");
                    if (processes.Length == 1)
                    {
                        var process = processes[0];
                        _running.Add(service, process.CloseMainWindow());
                        tasks.Add(processes[0].WaitForExitAsync());
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{service} FAILED TO STOP");
                }

            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public void StartServices() 
        {
            foreach (var service in _running)
            {
                if (service.Value)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = _startUpFolder + service.Key + ".bat",
                        UseShellExecute = true,
                    });
                }
            }

            _running.Clear();
        }
    }
}
