using Discord;
using Discord.WebSocket;
using GameServer.Discord;
using GameServer.GameServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddLog4Net("log4net.config", false);
builder.Services.AddLogging();


builder.Services.AddSingleton<IServerManager, ServerManager>();

builder.Services.AddOptions<ServerManagerConfig>()
    .Bind(builder.Configuration.GetRequiredSection(ServerManagerConfig.SectionName))
    .ValidateOnStart()
    .ValidateDataAnnotations();

// Add Windows Service support
builder.Services.AddHostedService<GameWorker>();
builder.Services.AddWindowsService();


// Add Health Controller
builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8069);
});

// Add Discord client and health checker
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds
}));
builder.Services.AddSingleton<IHealthChecker, HealthChecker>();
builder.Services.AddHostedService<DiscordWorker>();


var host = builder.Build();

host.MapControllers();

host.Run();
