# Install GameServer as a Windows Service
# Run this script as Administrator

param(
	[Parameter(Mandatory=$true)]
	[string]$ServicePath,

	[string]$ServiceName = "GameServer",
	[string]$DisplayName = "Game Server",
	[string]$Description = "Background service for Game Server"
)

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
	Write-Error "This script must be run as Administrator!"
	exit 1
}

# Verify the service executable exists
if (-not (Test-Path "$ServicePath\GameServer.exe")) {
	Write-Error "GameServer.exe not found at: $ServicePath\GameServer.exe"
	exit 1
}

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
	Write-Warning "Service '$ServiceName' already exists. Removing it first..."

	# Stop the service if it's running
	if ($existingService.Status -eq "Running") {
		Stop-Service -Name $ServiceName -Force
		Start-Sleep -Seconds 2
	}

	# Remove the service
	sc.exe delete $ServiceName
	Start-Sleep -Seconds 2
}

# Create the new service
Write-Host "Creating service: $ServiceName at $ServicePath"
New-Service -Name $ServiceName `
	-BinaryPathName "$ServicePath\GameServer.exe" `
	-DisplayName $DisplayName `
	-Description $Description `
	-StartupType Automatic

# Configure recovery settings (restart service on failure)
Write-Host "Configuring recovery settings..."
sc.exe failure $ServiceName reset= 60 actions= restart/5000

# Start the service
Write-Host "Starting service..."
Start-Service -Name $ServiceName

# Verify it started
Start-Sleep -Seconds 2
$service = Get-Service -Name $ServiceName
if ($service.Status -eq "Running") {
	Write-Host "✓ Service '$ServiceName' installed and started successfully!" -ForegroundColor Green
	Write-Host "Service is set to start automatically on server reboot."
} else {
	Write-Error "✗ Service failed to start. Check Windows Event Viewer for details."
	exit 1
}
