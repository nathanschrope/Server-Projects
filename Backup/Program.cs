using Backup;
using Backup.StartStop;
using System.Xml.Serialization;

Config config = new Config()
{
    BackupConfig = new BackupConfig()
    {
        BackupPath = Directory.GetCurrentDirectory(),
        FileCount = 1
    },
    StartStopConfig = new StartStopConfig()
};

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
builder.Services.AddSingleton(x =>
{
    var logger = x.GetService<ILogger<BackupService>>();
    return logger == null
        ? throw new Exception()
        : (IBackupService)ActivatorUtilities.CreateInstance<BackupService>(x, logger, config.BackupConfig);
}
);
builder.Services.AddSingleton(x =>
{
    var logger = x.GetService<ILogger<StartAndStopService>>();
    return logger == null
        ? throw new Exception()
        : (IStartStopService)ActivatorUtilities.CreateInstance<StartAndStopService>(x, logger, config.StartStopConfig);
}
);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();