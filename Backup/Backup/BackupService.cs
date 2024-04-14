using CommonLibrary.Backup;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Backup
{
    public class BackupService : IBackupService
    {
        private readonly ILogger<BackupService> _logger;
        private const string DATETIME_PATTERN = "yyyyMMdd";
        private IConfig<IApplication> _config;

        public BackupService(ILogger<BackupService> logger, IConfig<IApplication> config)
        {
            _logger = logger;
            _config = config;

            _logger.LogInformation($"Backup + FileCount: {config.MaximumBackupFilePerApplication}");
            _logger.LogInformation($"Backup + BackupPath: {config.BackUpPath}");
            _logger.LogInformation($"Backup + Directories Count: {config.Applications.Count()}");
            foreach (var app in config.Applications)
            {
                _logger.LogInformation($"\t {app}");
            }
        }

        public void Backup(IApplication app)
        {
            if (!Directory.Exists(_config.BackUpPath))
                Directory.CreateDirectory(_config.BackUpPath);


            foreach (var dir in app.Directories)
            {

                if (Directory.Exists(dir))
                {
                    try
                    {
                        _logger.LogInformation($"Getting Backup of {dir} to {_config.BackUpPath}");
                        var dirInfo = new DirectoryInfo(dir);
                        var fileName = "Backup_" + dirInfo.Name + "_" + DateTime.Now.ToString(DATETIME_PATTERN) + ".zip";
                        if (!File.Exists(_config.BackUpPath + "\\" + fileName))
                            ZipFile.CreateFromDirectory(dir, _config.BackUpPath + "\\" + fileName);
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

        public void Cleanup(IApplication app)
        {
            //cleanup if needed

            foreach (var dir in app.Directories)
            {
                if (Directory.Exists(dir))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var files = Directory.GetFiles(_config.BackUpPath, "Backup_" + dirInfo.Name + "_*.zip");
                    if (files.Length > _config.MaximumBackupFilePerApplication) //some have to go
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

                        for (int i = 0; i < sortedList.Count - _config.MaximumBackupFilePerApplication; i++)
                        {
                            var fileName = $"\\Backup_{dirInfo.Name}_{sortedList[0].ToString(DATETIME_PATTERN)}.zip";
                            _logger.LogInformation($"Deleting {fileName}");

                            File.Delete(_config.BackUpPath + fileName);
                        }
                    }
                }
            }

        }
    }
}
