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