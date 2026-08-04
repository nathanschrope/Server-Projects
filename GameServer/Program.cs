using GameServer;

var builder = Host.CreateApplicationBuilder(args);

// Add Windows Service support
builder.Services.AddWindowsService();

builder.Services.AddOptions<ServerManagerConfig>()
    .Bind(builder.Configuration.GetRequiredSection(ServerManagerConfig.SectionName))
    .ValidateOnStart()
    .ValidateDataAnnotations();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
