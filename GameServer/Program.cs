using GameServer;

var builder = Host.CreateApplicationBuilder(args);

// Add Windows Service support
builder.Services.AddWindowsService();

builder.Services.AddOptions<GameProcessManagerConfiguration>()
    .Bind(builder.Configuration.GetRequiredSection(GameProcessManagerConfiguration.SectionName))
    .ValidateOnStart()
    .ValidateDataAnnotations();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
