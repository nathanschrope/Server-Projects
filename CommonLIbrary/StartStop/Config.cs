namespace CommonLibrary.StartStop
{

    public class Config : IConfig<Application>
    {
        public string StartUpFolder { get; set; } = "";
        public List<Application> Applications { get; set; } = [];
    }

    public interface IConfig<T> where T : IApplication
    {
        public string StartUpFolder { get; set; }
        public List<T> Applications { get; set; }
    }
}
