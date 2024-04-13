namespace CommonLibrary.Backup
{
    public class Config : IConfig<Application>
    {
        public int MaximumBackupFilePerApplication { get; set; } = 3;
        public string BackUpPath { get; set; } = "";
        public List<Application> Applications { get; set; } = [];
    }

    public interface IConfig<T> where T : IApplication
    {
        public int MaximumBackupFilePerApplication { get; set; }
        public string BackUpPath {  get; set; }

        public List<T> Applications { get; set; }
    }
}
