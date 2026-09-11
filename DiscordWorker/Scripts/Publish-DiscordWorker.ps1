# Script to publish DiscordWorker
param(
	[string]$Configuration = "Release",
	[string]$OutputPath = "$PSScriptRoot\..\bin\publish"
)

Write-Host "Publishing DiscordWorker ($Configuration)..."

$projectPath = Get-Item -Path "$PSScriptRoot\..\DiscordWorker.csproj"

dotnet publish $projectPath.FullName `
	-c $Configuration `
	-o $OutputPath `
	--self-contained `
	--runtime win-x64

if ($LASTEXITCODE -eq 0) {
	Write-Host "Publish completed successfully."
	Write-Host "Output location: $OutputPath"
} else {
	Write-Host "Publish failed with exit code: $LASTEXITCODE"
}
