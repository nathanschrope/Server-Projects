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
            do
            {
                List<Task> tasks = new List<Task>();
                foreach (var app in _config.Applications)
                {
                    tasks.Add(StopBackupStart(app));
                }

                await Task.WhenAll(tasks);

                //setup wait
                var now = DateTime.UtcNow;
                var nextBackupTime = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
                if (nextBackupTime.CompareTo(now) <= 0)
                {//in past
                    nextBackupTime = nextBackupTime.AddDays(1);
                }

                _logger.LogInformation($"Next Backup time: {nextBackupTime}");

                await Task.Delay(new TimeSpan(nextBackupTime.Ticks - now.Ticks));
            }
            while (!stoppingToken.IsCancellationRequested);
        }

        public async Task StopBackupStart(Application app)
        {
            _logger.LogInformation($"Stopping {app.Name} at {DateTime.UtcNow}");
            await _startAndStopService.StopServicesAsync(app).ConfigureAwait(false);

            _logger.LogInformation($"Starting Backup for {app.Name} at {DateTime.UtcNow}");
            _backupService.Backup(app);

            _logger.LogInformation($"Starting {app.Name} at {DateTime.UtcNow}");
            _startAndStopService.StartServices(app);

            _logger.LogInformation($"Starting cleanup for {app.Name} at {DateTime.UtcNow}");
            _backupService.Cleanup(app);
        }
    }
}
