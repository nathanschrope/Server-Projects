using Discord;
using Discord.WebSocket;

namespace GameServer.Discord;

internal class DiscordWorker : BackgroundService
{
    private ILogger _logger;
    private DiscordSocketClient _client;
    private IHealthChecker _healthChecker;
    private string _token { get; }

    public DiscordWorker(ILogger<DiscordWorker> logger, DiscordSocketClient client, IHealthChecker healthChecker)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(healthChecker);

        _logger = logger;
        _client = client;
        _healthChecker = healthChecker;

        // Ideally, load from configuration or environment variable
        _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
                 ?? throw new InvalidOperationException("DISCORD_TOKEN not set.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Hook up events
        _client.Log += LogAsync;
        _client.Ready += OnReadyAsync;
        _client.MessageReceived += OnMessageReceivedAsync;


        try
        {
            await _client.LoginAsync(TokenType.Bot, _token).ConfigureAwait(false);
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Discord bot service is stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in the Discord bot service.");
        }
        finally
        {
            await _client.StopAsync();
            await _client.LogoutAsync();
        }
    }

    private Task LogAsync(LogMessage msg)
    {
        _logger.LogInformation("[{Source}] {Message}", msg.Source, msg.Message);
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        _logger.LogInformation("Bot connected as {Username}#{Discriminator}",
            _client.CurrentUser.Username, _client.CurrentUser.Discriminator);

        return DoHealthChecksForeverAsync();
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        // Ignore bot messages
        if (message.Author.IsBot) return;

        if (message.Content.Equals("!ping", StringComparison.OrdinalIgnoreCase))
        {
            await message.Channel.SendMessageAsync("Pong!");
        }
    }

    private async Task DoHealthChecksForeverAsync()
    {
        while (true)
        {
            var messages = await _healthChecker.GetHealthAsync(CancellationToken.None);

            if (messages.Count != 0)
            {
                foreach (var guild in _client.Guilds)
                {
                    var channels = guild.Channels.Where(x => x.GetChannelType() == ChannelType.Text && x.Users.Where(y => y.Id == 1227776913272209478).Any());

                    if (channels.Count() > 1)
                    {
                        var botChannels = channels.Where(x => x.Name.Equals("bot", StringComparison.OrdinalIgnoreCase));
                        if (botChannels.Any())
                            channels = botChannels;
                        else
                        {
                            var generalChannels = channels.Where(x => x.Name.Equals("general", StringComparison.OrdinalIgnoreCase));
                            if (generalChannels.Any())
                                channels = generalChannels;
                        }

                    }

                    foreach (ITextChannel channel in channels)
                    {
                        foreach (var message in messages)
                        {
                            try
                            {
                                await channel.SendMessageAsync(message, false, null, new RequestOptions()
                                {
                                    RetryMode = RetryMode.AlwaysFail
                                });
                            }
                            catch { }
                        }
                    }
                }
            }
            await Task.Delay(15000);
        }
    }
}
