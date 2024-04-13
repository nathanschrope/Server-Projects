using System.Xml.Serialization;

namespace CommonLibrary.XML
{
    public class Application : Backup.IApplication, StartStop.IApplication
    {
        [XmlArray]
        [XmlArrayItem("Folder")]
        public List<string> Directories { get; set; } = [];

        [XmlAttribute]
        public string Name { get; set; } = "";

        [XmlAttribute]
        public string CheckTitle { get; set; } = "";
    }
}
