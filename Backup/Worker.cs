using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Backup
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly List<string> _directories = new List<string>()
        {
            "C:\\Steam",
            "C:\\Users\\admin\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup"
        };
        private readonly string _backupPath = "\\\\10.0.0.14\\NetworkStorage\\ServerBackup";
        private const int BACKUP_COUNT = 3;
        private const string DATETIME_PATTERN = "yyyyMMdd";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                try
                {
                    if (!Directory.Exists(_backupPath))
                        Directory.CreateDirectory(_backupPath);

                    //setup wait
                    var now = DateTime.UtcNow;
                    var nextBackupTime = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
                    if (nextBackupTime.CompareTo(now) <= 0)
                    {//in past
                        nextBackupTime = nextBackupTime.AddDays(1);
                    }
#if DEBUG
                    _logger.Log(LogLevel.Warning, "DEBUG MODE: WILL LOG IN 10 SECONDS.");
                    nextBackupTime = DateTime.UtcNow.AddSeconds(10);
#endif

                    _logger.LogInformation($"Next Backup time: {nextBackupTime}");

                    await Task.Delay(new TimeSpan(nextBackupTime.Ticks - now.Ticks));

                    //do task
                    foreach (var dir in _directories)
                    {
                        if (Directory.Exists(dir))
                        {
                            _logger.LogInformation($"Getting Backup of {dir} to {_backupPath}");
                            var dirInfo = new DirectoryInfo(dir);
                            var fileName = "Backup_" + DateTime.Now.ToString(DATETIME_PATTERN) + "_" + dirInfo.Name + ".zip";
                            if (!File.Exists(_backupPath + "\\" + fileName))
                                ZipFile.CreateFromDirectory(dir, _backupPath + "\\" + fileName);
                        }
                        else
                        {
                            _logger.LogWarning($"Directory does not exist: {dir}");
                        }
                    }

                    //cleanup if needed
                    foreach (var dir in _directories)
                    {
                        if (Directory.Exists(dir))
                        {
                            var dirInfo = new DirectoryInfo(dir);
                            var files = Directory.GetFiles(_backupPath, "Backup_*_" + dirInfo.Name + ".zip");
                            if (files.Length > BACKUP_COUNT) //some have to go
                            {
                                var sortedList = new List<DateTime>();
                                foreach (var zip in files)
                                {
                                    Regex regex = new Regex($"^.*\\Backup_(?<Date>\\d*)_{dirInfo.Name}.zip");
                                    var matches = regex.Match(zip).Groups;
                                    if (matches.ContainsKey("Date"))
                                    {
                                        try
                                        {
                                            sortedList.Add(DateTime.ParseExact(matches.GetValueOrDefault("Date").Value, DATETIME_PATTERN, null));
                                        }
                                        catch { }
                                    }
                                }

                                sortedList.Sort();

                                for (int i = 0; i < sortedList.Count - BACKUP_COUNT; i++)
                                {
                                    var fileName = $"\\Backup_{sortedList[0].ToString(DATETIME_PATTERN)}_{dirInfo.Name}.zip";
                                    _logger.LogInformation($"Deleting {fileName}");
                                    File.Delete(_backupPath + fileName);
                                }
                            }
                        }
                    }


                }
                catch (Exception e)
                {
                    _logger.LogError(e, "TODAY FAILED");
                }
            }
            while (!stoppingToken.IsCancellationRequested);
        }
    }
}
