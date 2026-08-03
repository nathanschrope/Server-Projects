# Publish GameServer for Windows Service deployment
# This script builds and publishes the GameServer as a self-contained Windows x64 application

param(
	[string]$Configuration = "Release",
	[string]$OutputPath = "$PSScriptRoot\..\GameServer-Published"
)

Write-Host "Publishing GameServer for Windows Service..."
Write-Host "Configuration: $Configuration"
Write-Host "Output Path: $OutputPath"

# Get the GameServer project path
$gameServerProject = "$PSScriptRoot\..\GameServer\GameServer.csproj"

if (-not (Test-Path $gameServerProject)) {
	Write-Error "GameServer.csproj not found at: $gameServerProject"
	exit 1
}

# Publish the application
dotnet publish $gameServerProject `
	-c $Configuration `
	-r win-x64 `
	--self-contained `
	-o $OutputPath

if ($LASTEXITCODE -eq 0) {
	Write-Host "✓ Published successfully to: $OutputPath" -ForegroundColor Green
	Write-Host ""
	Write-Host "Next steps:"
	Write-Host "1. Copy the contents of '$OutputPath' to your Windows Server"
	Write-Host "2. Run (as Administrator on the server):"
	Write-Host "   .\Install-GameServerService.ps1 -ServicePath 'C:\Services\GameServer'"
} else {
	Write-Error "✗ Publishing failed."
	exit 1
}
