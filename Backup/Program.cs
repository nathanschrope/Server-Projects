using Backup;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<BackupService>();
builder.Services.AddSingleton<StartAndStopService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();