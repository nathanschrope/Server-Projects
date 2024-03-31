using System.Diagnostics;

namespace Backup
{
    public class StartAndStopService
    {
        private readonly ILogger _logger;
        private List<Service> _services;
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
            _services.Add(new Service("enshrouded_server"));
            _services.Add(new Service("SonsOfTheForestDS"));
            _services.Add(new Service("valheim_server"));
            _services.Add(new Service("Minecraft") { CheckTitle="java"});
        }

        public async Task StopServicesAsync()
        {
            var tasks = new List<Task>();
            foreach (var service in _services)
            {
                try
                {
                    var processes = Process.GetProcessesByName(service.Name);
                    _logger.LogInformation($"1. {service.Name} found {processes.Length} services");

                    if (processes.Length != 1 && !String.IsNullOrEmpty(service.CheckTitle))
                    {
                        processes = Process.GetProcessesByName(service.CheckTitle);
                        _logger.LogInformation($"2. {service.Name} found {processes.Length} services");
                        processes = processes.Where(x => x.MainWindowTitle.Equals(service.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                    }


                    if (processes.Length == 1)
                    {
                        var process = processes[0];

                        var wasClosed = process.CloseMainWindow();
                        _logger.LogInformation($"{service.Name} closing: {wasClosed}");

                        if (wasClosed)
                        {
                            _running.Add(service.Name, true);
                            tasks.Add(processes[0].WaitForExitAsync());
                        }
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
