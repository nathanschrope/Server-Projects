﻿using CommonLibrary.StartStop;
using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;
using Task = System.Threading.Tasks.Task;

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

        public async Task<bool> StopServicesAsync(IApplication app)
        {
            var tasks = new List<Task>();

            try
            {
                var processes = Process.GetProcessesByName(app.Name);
                _logger.LogInformation($"1. {app.Name} found {processes.Length} Applications");

                if (processes.Length != 1 && !String.IsNullOrEmpty(app.CheckTitle))
                {
                    processes = Process.GetProcessesByName(app.CheckTitle);
                    _logger.LogInformation($"2. {app.Name} found {processes.Length} Applications");
                    processes = processes.Where(x => x.MainWindowTitle.Equals(app.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                }


                if (processes.Length == 1)
                {
                    var process = processes[0];

                    var wasClosed = process.CloseMainWindow();
                    _logger.LogInformation($"{app.Name} closing: {wasClosed}");

                    if (wasClosed)
                    {
                        _running.Add(app.Name, true);
                        tasks.Add(processes[0].WaitForExitAsync());
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{app} FAILED TO STOP");
            }


            await Task.WhenAll(tasks).ConfigureAwait(false);

            return tasks.Count > 0;
        }

        public void StartServices(IApplication app)
        {
            try
            {
                using (TaskService svc = new TaskService())
                {
                    var task = svc.FindTask(app.Name);
                    
                    if(task != null)
                    {
                        task.Run();
                        _logger.LogInformation($"{app.Name} was started using Scheduled Task");
                    }
                        
                }
            }catch(Exception e)
            {
                _logger.LogError(e, $"TASK NOT FOUND: {app.Name}");
            }
        }
    }
}
