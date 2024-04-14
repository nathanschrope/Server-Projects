using CommonLibrary.StartStop;

namespace Backup.StartStop
{
    public interface IStartStopService
    {
        Task StopServicesAsync(IApplication app);
        void StartServices(IApplication app);
    }
}
