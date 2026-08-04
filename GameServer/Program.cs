using GameServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddSingleton<IServerManager, ServerManager>();

builder.Services.AddOptions<ServerManagerConfig>()
    .Bind(builder.Configuration.GetRequiredSection(ServerManagerConfig.SectionName))
    .ValidateOnStart()
    .ValidateDataAnnotations();

// Add Windows Service support
builder.Services.AddHostedService<Worker>();
builder.Services.AddWindowsService();

builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8069);
});

var host = builder.Build();

host.MapControllers();

host.Run();
