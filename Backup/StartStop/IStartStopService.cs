namespace Backup.StartStop
{
    public interface IStartStopService
    {
        Task StopServicesAsync();
        void StartServices();
    }
}
