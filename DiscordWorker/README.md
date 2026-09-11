# DiscordWorker Service

The Discord Worker Service is a standalone Windows Service that monitors the health of the GameServer and reports status changes to Discord channels.

## Purpose

This service runs independently from the GameServer process to ensure that Discord health notifications are sent even when GameServer restarts. It polls the GameServer's health endpoint and sends alerts to configured Discord channels when server status changes occur.

## Prerequisites

- .NET 10.0 runtime or SDK
- Discord Bot token (set via `DISCORD_TOKEN` environment variable)
- GameServer running and accessible at `http://localhost:8069/server/health`

## Installation

### Publishing the Service

Run the publish script to build the service:

```powershell
.\Scripts\Publish-DiscordWorker.ps1
```

This will create a self-contained executable in `bin\publish`.

### Installing as Windows Service

After publishing, run the install script with administrator privileges:

```powershell
.\Scripts\Install-DiscordWorkerService.ps1
```

This script will:
- Stop and remove any existing DiscordWorker service
- Install DiscordWorker as a Windows Service with automatic startup
- Start the service

### Setting Environment Variables

The service requires the Discord bot token. Set it as an environment variable before starting:

```powershell
[Environment]::SetEnvironmentVariable("DISCORD_TOKEN", "your-bot-token", "Machine")
```

You may need to restart the service after setting this variable.

## Uninstallation

To remove the service, run with administrator privileges:

```powershell
.\Scripts\Uninstall-DiscordWorkerService.ps1
```

This will:
- Stop the DiscordWorker service
- Remove it from Windows Services

## Configuration

The service uses the following configuration files:

- `appsettings.json` - Main configuration
- `appsettings.Development.json` - Development-specific overrides
- `log4net.config` - Logging configuration

### Log Locations

Logs are written to:
- `logs/DiscordWorker.log` - All service logs

## Features

- Polls GameServer health endpoint every 15 seconds
- Reports server status changes to Discord
- Targets specific Discord channels (`#bot` or `#general`)
- Responds to `!ping` messages
- Logs all activities via log4net

## Troubleshooting

### Service Won't Start

1. Check that `DISCORD_TOKEN` environment variable is set
2. Verify GameServer is running at `http://localhost:8069`
3. Check `logs/DiscordWorker.log` for detailed error messages

### Discord Bot Not Connecting

- Verify the Discord token is correct and valid
- Ensure the bot has permissions in the Discord server
- Check logs for authentication errors

### Missing Health Updates

- Verify GameServer is running and responding to health checks
- Check network connectivity between DiscordWorker and GameServer
- Ensure the Discord bot has permissions to send messages in target channels

## Running in Development

For local testing without installing as a service:

```powershell
cd DiscordWorker
$env:DISCORD_TOKEN = "your-bot-token"
dotnet run
```

## Related Services

- **GameServer** - The main game server process manager. Exposes health endpoint at port 8069.
- Both services should run independently for reliability. DiscordWorker's health checks will work as long as GameServer is accessible via HTTP.
