namespace CommonLibrary.StartStop
{
    public class Application : IApplication
    {
        public string Name { get; set; } = "";
        public string CheckTitle { get; set; } = "";
    }

    public interface IApplication
    {
        public string Name { get; set; }
        public string CheckTitle { get; set; }
    }
}
