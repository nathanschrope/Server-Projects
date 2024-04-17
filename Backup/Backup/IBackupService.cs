using CommonLibrary.Backup;

namespace Backup
{
    public interface IBackupService
    {
        void Backup(IApplication app);
        void Backup(string path);
        void Cleanup(IApplication app);
    }
}
