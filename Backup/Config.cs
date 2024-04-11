using Backup.StartStop;

namespace Backup
{
    public class Config
    {
        public BackupConfig BackupConfig { get; set; } = new BackupConfig();

        public StartStopConfig StartStopConfig { get; set; } = new StartStopConfig();
    }
}
