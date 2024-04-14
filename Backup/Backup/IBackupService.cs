using CommonLibrary.Backup;

namespace Backup
{
    public interface IBackupService
    {
        void Backup(IApplication app);
        void Cleanup(IApplication app);
    }
}
