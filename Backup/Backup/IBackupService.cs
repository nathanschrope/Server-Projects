namespace Backup
{
    public interface IBackupService
    {
        void Backup();
        void Cleanup();
    }
}
