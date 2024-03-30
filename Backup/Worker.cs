namespace Backup
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private BackupService _backupService;
        private StartAndStopService _startAndStopService;


        public Worker(ILogger<Worker> logger, BackupService backupService, StartAndStopService startAndStopService)
        {
            _logger = logger;
            _backupService = backupService;
            _startAndStopService = startAndStopService;
            configureServers();
        }

        public void configureServers()
        {

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                await _startAndStopService.StopServicesAsync().ConfigureAwait(false);

                _backupService.Backup();

                _startAndStopService.StartServices();

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
    }
}
