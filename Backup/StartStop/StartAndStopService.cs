using CommonLibrary.StartStop;
using System.Diagnostics;

namespace Backup.StartStop
{
    public class StartAndStopService : IStartStopService
    {
        private readonly ILogger _logger;
        private readonly IConfig<IApplication> _config;
        private readonly Dictionary<string, bool> _running;
        public StartAndStopService(ILogger<StartAndStopService> logger, IConfig<IApplication> config) 
        { 
            _logger = logger;
            _config = config;
            _running = [];

            //Replace %AppData%
            _config.StartUpFolder = Environment.ExpandEnvironmentVariables(_config.StartUpFolder);

            _logger.LogInformation($"StartStop + StartUpFolder: {_config.StartUpFolder}");
            _logger.LogInformation($"StartStop + Service Count: {_config.Applications.Count()}");

            foreach (var ser in config.Applications)
            {
                _logger.LogInformation($"\t {ser.Name} ({ser.CheckTitle})");
            }
        }

        public async Task StopServicesAsync()
        {
            var tasks = new List<Task>();
            foreach (var service in _config.Applications)
            {
                try
                {
                    var processes = Process.GetProcessesByName(service.Name);
                    _logger.LogInformation($"1. {service.Name} found {processes.Length} Applications");

                    if (processes.Length != 1 && !String.IsNullOrEmpty(service.CheckTitle))
                    {
                        processes = Process.GetProcessesByName(service.CheckTitle);
                        _logger.LogInformation($"2. {service.Name} found {processes.Length} Applications");
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
                        FileName = _config.StartUpFolder + service.Key + ".bat",
                        UseShellExecute = true,
                    });
                }
            }

            _running.Clear();
        }
    }
}
