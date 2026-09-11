# Script to install DiscordWorker as a Windows Service
param(
	[string]$ServiceName = "DiscordWorker",
	[string]$DisplayName = "Discord Worker Service",
	[string]$ExePath = (Get-Item -Path $PSScriptRoot\DiscordWorker.exe).FullName
)

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService) {
	Write-Host "Service '$ServiceName' already exists. Stopping and removing..."
	Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
	Start-Sleep -Seconds 2
	Remove-Service -Name $ServiceName -ErrorAction SilentlyContinue
	Start-Sleep -Seconds 2
}

# Install service
Write-Host "Installing '$ServiceName' service..."
New-Service -Name $ServiceName `
	-DisplayName $DisplayName `
	-BinaryPathName $ExePath `
	-StartupType Automatic `
	-ErrorAction Stop

# Start the service
Write-Host "Starting '$ServiceName' service..."
Start-Service -Name $ServiceName

# Verify installation
$service = Get-Service -Name $ServiceName
Write-Host "Service installation completed. Status: $($service.Status)"
