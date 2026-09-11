# Script to uninstall DiscordWorker Windows Service
param(
	[string]$ServiceName = "DiscordWorker"
)

# Check if service exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService) {
	Write-Host "Stopping '$ServiceName' service..."
	Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
	Start-Sleep -Seconds 2

	Write-Host "Removing '$ServiceName' service..."
	Remove-Service -Name $ServiceName -ErrorAction SilentlyContinue

	Write-Host "Service uninstall completed."
} else {
	Write-Host "Service '$ServiceName' not found."
}
