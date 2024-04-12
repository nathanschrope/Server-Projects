using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Backup
{
    public class BackupService : IBackupService
    {
        private readonly ILogger<BackupService> _logger;
        private const string DATETIME_PATTERN = "yyyyMMdd";
        private BackupConfig _config;

        public BackupService(ILogger<BackupService> logger, BackupConfig config)
        {
            _logger = logger;
            _config = config;

            _logger.LogInformation($"Backup + FileCount: {config.FileCount}");
            _logger.LogInformation($"Backup + BackupPath: {config.BackupPath}");
            _logger.LogInformation($"Backup + Directories Count: {config.Directories.Count}");
            foreach (var dir in config.Directories)
            {
                _logger.LogInformation($"\t {dir}");
            }
        }

        public void Backup()
        {
            if (!Directory.Exists(_config.BackupPath))
                Directory.CreateDirectory(_config.BackupPath);

            foreach (var dir in _config.Directories)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        _logger.LogInformation($"Getting Backup of {dir} to {_config.BackupPath}");
                        var dirInfo = new DirectoryInfo(dir);
                        var fileName = "Backup_" + dirInfo.Name + "_" + DateTime.Now.ToString(DATETIME_PATTERN) + ".zip";
                        if (!File.Exists(_config.BackupPath + "\\" + fileName))
                            ZipFile.CreateFromDirectory(dir, _config.BackupPath + "\\" + fileName);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"ZIP FAILED {dir}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Directory does not exist: {dir}");
                }
            }
        }

        public void Cleanup()
        {
            //cleanup if needed
            foreach (var dir in _config.Directories)
            {
                if (Directory.Exists(dir))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var files = Directory.GetFiles(_config.BackupPath, "Backup_" + dirInfo.Name + "_*.zip");
                    if (files.Length > _config.FileCount) //some have to go
                    {
                        var sortedList = new List<DateTime>();
                        foreach (var zip in files)
                        {
                            Regex regex = new Regex($"^.*\\Backup_{dirInfo.Name}_(?<Date>\\d*).zip");
                            var matches = regex.Match(zip).Groups;
                            if (matches.ContainsKey("Date"))
                            {
                                try
                                {
                                    var match = matches.GetValueOrDefault("Date");
                                    if (match != null)
                                        sortedList.Add(DateTime.ParseExact(match.Value, DATETIME_PATTERN, null));
                                }
                                catch { }
                            }
                        }

                        sortedList.Sort();

                        for (int i = 0; i < sortedList.Count - _config.FileCount; i++)
                        {
                            var fileName = $"\\Backup_{dirInfo.Name}_{sortedList[0].ToString(DATETIME_PATTERN)}.zip";
                            _logger.LogInformation($"Deleting {fileName}");

                            File.Delete(_config.BackupPath + fileName);
                        }
                    }
                }
            }
        }
    }
}
