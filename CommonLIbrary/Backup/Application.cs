namespace CommonLibrary.Backup
{
    public class Application: IApplication
    {
        public List<string> Directories { get; set; } = [];
    }
    
    public interface IApplication
    { 
        public List<string> Directories { get; set; }
    }
}
