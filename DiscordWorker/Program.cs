using Discord;
using Discord.WebSocket;
using DiscordWorker;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddLog4Net("log4net.config", false);
    })
    .ConfigureServices(services =>
    {
        // Add Discord client and health checker
        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        }));
        services.AddSingleton<IHealthChecker, HealthChecker>();
        services.AddHostedService<DiscordWorkerService>();
    })
    .UseWindowsService();

var host = builder.Build();
await host.RunAsync();
