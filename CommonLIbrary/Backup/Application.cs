namespace CommonLibrary.Backup
{
    public class Application: IApplication
    {
        public string Name { get; set; } = "";
        public List<string> Directories { get; set; } = [];
    }
    
    public interface IApplication
    {
        public string Name { get; set; }
        public List<string> Directories { get; set; }
    }
}
