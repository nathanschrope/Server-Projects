# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
	Write-Error "This script must be run as Administrator!"
	exit 1
}

$gameServerName = 'GameServer'
$gameServerService = Get-Service -Name $gameServerName -ErrorAction SilentlyContinue
if (-not $gameServerService) {
	Write-Warning "Service '$gameServerName' does not exist."
} else {

	# Stop the service if it's running
	if ($gameServerService.Status -eq "Running") {
		Write-Host "Stopping service: $gameServerName"
		Stop-Service -Name $gameServerName -Force
		Start-Sleep -Seconds 2
	}

	# Remove the service
	Write-Host "Uninstalling service: $gameServerName"
	sc.exe delete $gameServerName
	Start-Sleep -Seconds 2

	$gameCheck = Get-Service -Name $gameServerName -ErrorAction SilentlyContinue
	if (-not $gameCheck) {
		Write-Host "Service '$gameServerName' uninstalled successfully!" -ForegroundColor Green
	} else {
		Write-Error "Failed to uninstall service. It may be locked."
		exit 1
	}
}

# Check if service exists
$discordWorkerName = 'DiscordWorker'
$discordWorkerService = Get-Service -Name $discordWorkerName -ErrorAction SilentlyContinue
if (-not $discordWorkerService) {
	Write-Warning "Service '$discordWorkerName' does not exist."
} else {

	# Stop the service if it's running
	if ($discordWorkerService.Status -eq "Running") {
		Write-Host "Stopping service: $discordWorkerName"
		Stop-Service -Name $discordWorkerName -Force
		Start-Sleep -Seconds 2
	}

	# Remove the service
	Write-Host "Uninstalling service: $discordWorkerName"
	sc.exe delete $discordWorkerName
	Start-Sleep -Seconds 2

	$discordCheck = Get-Service -Name $discordWorkerName -ErrorAction SilentlyContinue
	if (-not $discordCheck) {
		Write-Host "Service '$discordWorkerName' uninstalled successfully!" -ForegroundColor Green
	} else {
		Write-Error "Failed to uninstall service. It may be locked."
		exit 1
	}
}

cd C:\Projects\Server
git pull origin main
dotnet build

cd C:\Projects\Server\GameServer
dotnet publish

cd C:\Projects\Server\DiscordWorker
dotnet publish

cd C:\Projects\Server

$discordPath = 'C:\Projects\Server\DiscordWorker\bin\Release\net10.0\publish\DiscordWorker.exe'
if (-not (Test-Path $discordPath)) {
	Write-Error "DiscordWorker.exe not found at: $discordPath"
	exit 1
}

# adding discord service
New-Service -Name $discordWorkerName -BinaryPathName $discordPath -DisplayName 'Discord Worker' -Description 'Discord Worker' -StartupType Automatic
Write-Host "Configuring recovery settings..."
sc.exe failure $discordWorkerName reset= 60 actions= restart/5000
Write-Host "Starting $discordWorkerName..."
Start-Service -Name $discordWorkerName

$gamePath = 'C:\Projects\Server\GameServer\bin\Release\net10.0\publish\GameServer.exe'
if (-not (Test-Path $gamePath)) {
	Write-Error "GameServer.exe not found at: $gamePath"
	exit 1
}

# adding game service
New-Service -Name $gameServerName -BinaryPathName $gamePath -DisplayName 'Game Server' -Description 'Game Server' -StartupType Automatic
Write-Host "Configuring recovery settings..."
sc.exe failure $gameServerName reset= 60 actions= restart/5000
Write-Host "Starting $gameServerName..."
Start-Service -Name $gameServerName