namespace Backup
{
    public class BackupConfig
    {
        public int FileCount { get; set; }
        public string BackupPath { get; set; } = "";
        public List<string> Directories { get; set; } = new List<string>();
    }
}
