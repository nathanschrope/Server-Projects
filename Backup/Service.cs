namespace Backup
{
    internal class Service
    {
        public string Name { get; set; }
        public string CheckTitle { get; set; } = "";

        public Service() { }
        public Service(string name)
        {
            Name = name;
        }
    }
}
