using Backup.StartStop;
using CommonLibrary.XML;

namespace Backup
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IConfig _config;
        private IBackupService _backupService;
        private IStartStopService _startAndStopService;

        public Worker(ILogger<Worker> logger, IConfig config, IBackupService backupService, IStartStopService startAndStopService)
        {
            _logger = logger;
            _backupService = backupService;
            _startAndStopService = startAndStopService;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            List<Task> tasks = [];
            try
            {
                _backupService.Backup(_config.StartUpFolder);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "StartUpFolder Backup Failed");
            }

            foreach (var app in _config.Applications)
            {
                var task = StopBackupStartClean(app);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            tasks.Clear();

            _logger.LogInformation("DONE, WORKER CLOSING");
            Environment.Exit(0);
            return;
        }

        public async Task<bool> StopBackupStartClean(Application app)
        {
            var result = await StopAndBackup(app);
            await StartClean(app);
            return result;
        }

        public async Task<bool> StopAndBackup(Application app)
        {
            try
            {
                _logger.LogInformation($"Stopping {app.Name} at {DateTime.UtcNow}");
                var stopped = await _startAndStopService.StopServicesAsync(app).ConfigureAwait(false);

                _logger.LogInformation($"Starting Backup for {app.Name} at {DateTime.UtcNow}");
                _backupService.Backup(app);
                _logger.LogInformation($"Finished Backup for {app.Name} at {DateTime.UtcNow}");

                return stopped;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to Stop/Backup {app.Name}");
                return false;
            }
        }

        public Task StartClean(Application app)
        {
            try
            {


                _logger.LogInformation($"Starting {app.Name} at {DateTime.UtcNow}");
                _startAndStopService.StartServices(app);

                _logger.LogInformation($"Starting cleanup for {app.Name} at {DateTime.UtcNow}");
                _backupService.Cleanup(app);

                _logger.LogInformation($"Finished cleanup for {app.Name} at {DateTime.UtcNow}");

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to Start/Clean {app.Name}");
                return Task.CompletedTask;
            }
        }
    }
}
