using GameServer;

var builder = Host.CreateApplicationBuilder(args);

// Add Windows Service support
builder.Services.AddWindowsService();

// Configure GameServer options - configuration is automatically watched for changes
builder.Services.Configure<GameProcessManagerConfiguration>(
    builder.Configuration.GetSection(GameProcessManagerConfiguration.SectionName)
);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
