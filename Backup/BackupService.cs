﻿using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Backup
{
    public class BackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly List<string> _directories = new List<string>()
        {
            "C:\\Steam",
            "C:\\minecraft",
            "C:\\Users\\admin\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup",
            "C:\\Users\\admin\\AppData\\LocalLow\\IronGate\\Valheim",
            "C:\\Users\\admin\\AppData\\LocalLow\\Endnight\\SonsOfTheForestDS"
            //enshrouded is included in game files
        };
        private readonly string _backupPath = "\\\\10.0.0.14\\NetworkStorage\\ServerBackup";
        private const int BACKUP_COUNT = 3;
        private const string DATETIME_PATTERN = "yyyyMMdd";

        public BackupService(ILogger<BackupService> logger)
        {
            _logger = logger;
        }

        public void Backup()
        {
            if (!Directory.Exists(_backupPath))
                Directory.CreateDirectory(_backupPath);

            foreach (var dir in _directories)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        _logger.LogInformation($"Getting Backup of {dir} to {_backupPath}");
                        var dirInfo = new DirectoryInfo(dir);
                        var fileName = "Backup_" + dirInfo.Name + "_" + DateTime.Now.ToString(DATETIME_PATTERN) + ".zip";
                        if (!File.Exists(_backupPath + "\\" + fileName))
                            ZipFile.CreateFromDirectory(dir, _backupPath + "\\" + fileName);
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

            //cleanup if needed
            foreach (var dir in _directories)
            {
                if (Directory.Exists(dir))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var files = Directory.GetFiles(_backupPath, "Backup_" + dirInfo.Name + "_*.zip");
                    if (files.Length > BACKUP_COUNT) //some have to go
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
                                    sortedList.Add(DateTime.ParseExact(matches.GetValueOrDefault("Date").Value, DATETIME_PATTERN, null));
                                }
                                catch { }
                            }
                        }

                        sortedList.Sort();

                        for (int i = 0; i < sortedList.Count - BACKUP_COUNT; i++)
                        {
                            var fileName = $"\\Backup_{dirInfo.Name}_{sortedList[0].ToString(DATETIME_PATTERN)}.zip";
                            _logger.LogInformation($"Deleting {fileName}");
                            File.Delete(_backupPath + fileName);
                        }
                    }
                }
            }
        }
    }
}
