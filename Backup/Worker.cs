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
                int startAfterCount = _config.Applications.Where(x => x.AllStart).Count();
                int currentStartAfterCount = 0;
                List<Task> tasks = [];
                foreach (var app in _config.Applications)
                {
                    var task = StopAndBackup(app);
                    if (app.AllStart)
                    {
                        task = task.ContinueWith((x) =>
                            {
                                Interlocked.Increment(ref currentStartAfterCount);
                                return x.Result;
                            });
                    }

                    task = task.ContinueWith(async y =>
                    {
                        while (!Interlocked.Equals(currentStartAfterCount, startAfterCount))
                        {
                            await Task.Delay(60000);
                        }
                        return y.Result;
                    }).Unwrap()
                    .ContinueWith((z) => 
                    {
                        if (z.Result)
                        {
                            StartClean(app);
                        }
                        return z.Result;
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                tasks.Clear();
                currentStartAfterCount = 0;

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

        public async Task<bool> StopAndBackup(Application app)
        {
            _logger.LogInformation($"Stopping {app.Name} at {DateTime.UtcNow}");
            var stopped = await _startAndStopService.StopServicesAsync(app).ConfigureAwait(false);

            _logger.LogInformation($"Starting Backup for {app.Name} at {DateTime.UtcNow}");
            _backupService.Backup(app);
            _logger.LogInformation($"Finished Backup for {app.Name} at {DateTime.UtcNow}");

            return stopped;
        }

        public Task StartClean(Application app)
        {
            _logger.LogInformation($"Starting {app.Name} at {DateTime.UtcNow}");
            _startAndStopService.StartServices(app);

            _logger.LogInformation($"Starting cleanup for {app.Name} at {DateTime.UtcNow}");
            _backupService.Cleanup(app);

            _logger.LogInformation($"Finished cleanup for {app.Name} at {DateTime.UtcNow}");

            return Task.CompletedTask;
        }
    }
}
