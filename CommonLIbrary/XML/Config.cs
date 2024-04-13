using System.Xml.Serialization;

namespace CommonLibrary.XML
{
    public class Config : IConfig, Backup.IConfig<Application>, StartStop.IConfig<Application>
    {
        [XmlAttribute]
        public string Name { get; set; } = "Default";

        public int MaximumBackupFilePerApplication { get; set; } = 3;

        public string BackUpPath { get; set; } = "";

        [XmlArray]
        public List<Application> Applications { get; set; } = [];

        public string StartUpFolder { get; set; } = "";
    }

    public interface IConfig : Backup.IConfig<Backup.IApplication>, StartStop.IConfig<StartStop.IApplication>
    {
        public new List<XML.Application> Applications {get;set;}
        List<Backup.IApplication> Backup.IConfig<Backup.IApplication>.Applications
        {
            get { return Applications.Cast<Backup.IApplication>().ToList(); }
            set => throw new NotImplementedException();
        }
        List<StartStop.IApplication> StartStop.IConfig<StartStop.IApplication>.Applications { 
            get { return Applications.Cast<StartStop.IApplication>().ToList(); }
            set => throw new NotImplementedException(); 
        }
    }
}
