using CommonLibrary.StartStop;

namespace Backup.StartStop
{
    public interface IStartStopService
    {
        Task<bool> StopServicesAsync(IApplication app);
        void StartServices(IApplication app);
    }
}
