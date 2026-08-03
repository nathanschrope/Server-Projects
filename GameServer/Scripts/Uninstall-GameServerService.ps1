# Uninstall GameServer Windows Service
# Run this script as Administrator

param(
	[string]$ServiceName = "GameServer"
)

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
	Write-Error "This script must be run as Administrator!"
	exit 1
}

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
	Write-Warning "Service '$ServiceName' does not exist."
	exit 0
}

# Stop the service if it's running
if ($service.Status -eq "Running") {
	Write-Host "Stopping service: $ServiceName"
	Stop-Service -Name $ServiceName -Force
	Start-Sleep -Seconds 2
}

# Remove the service
Write-Host "Uninstalling service: $ServiceName"
sc.exe delete $ServiceName

Start-Sleep -Seconds 2
$remainingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $remainingService) {
	Write-Host "✓ Service '$ServiceName' uninstalled successfully!" -ForegroundColor Green
} else {
	Write-Error "✗ Failed to uninstall service. It may be locked."
	exit 1
}
