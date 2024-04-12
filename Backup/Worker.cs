using Backup.StartStop;

namespace Backup
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IBackupService _backupService;
        private IStartStopService _startAndStopService;


        public Worker(ILogger<Worker> logger, IBackupService backupService, IStartStopService startAndStopService)
        {
            _logger = logger;
            _backupService = backupService;
            _startAndStopService = startAndStopService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                //setup wait
                var now = DateTime.UtcNow;
                var nextBackupTime = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
                if (nextBackupTime.CompareTo(now) <= 0)
                {//in past
                    nextBackupTime = nextBackupTime.AddDays(1);
                }

                _logger.LogInformation($"Next Backup time: {nextBackupTime}");

                await Task.Delay(new TimeSpan(nextBackupTime.Ticks - now.Ticks));

                _logger.LogInformation($"Stopping services at {DateTime.UtcNow}");
                await _startAndStopService.StopServicesAsync().ConfigureAwait(false);

                _logger.LogInformation($"Starting Backup at {DateTime.UtcNow}");
                _backupService.Backup();

                _logger.LogInformation($"Starting services at {DateTime.UtcNow}");
                _startAndStopService.StartServices();

                _logger.LogInformation($"Starting cleanup at {DateTime.UtcNow}");
                _backupService.Cleanup();
            }
            while (!stoppingToken.IsCancellationRequested);
        }
    }
}
