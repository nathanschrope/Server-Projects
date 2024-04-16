using Backup;
using Backup.StartStop;
using CommonLibrary.XML;
using System.Xml.Serialization;

Config config = new Config();

if (args.Length > 1)
    throw new ArgumentException("Invalid number of Arguments");
else if (args.Length == 1)
{
    using (var reader = new StreamReader(args[0]))
    {
        var deserializedFile = new XmlSerializer(typeof(Config)).Deserialize(reader);
        if (deserializedFile != null)
            config = (Config)deserializedFile;
    }
}


var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy/MM/dd HH:mm:ss";
        options.IncludeScopes = false;
    });
builder.Services.AddSingleton(typeof(IConfig), config);
builder.Services.AddSingleton(typeof(CommonLibrary.StartStop.IConfig<CommonLibrary.StartStop.IApplication>), config);
builder.Services.AddSingleton(typeof(CommonLibrary.Backup.IConfig<CommonLibrary.Backup.IApplication>), config);
builder.Services.AddSingleton<IBackupService, BackupService>();
builder.Services.AddSingleton<IStartStopService, StartAndStopService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();