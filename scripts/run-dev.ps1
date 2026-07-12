$ErrorActionPreference = 'Stop'
$dotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

# Development uses Microsoft's trusted dotnet host and avoids producing a new
# unsigned app host on machines enforcing enterprise executable signing.
& $dotnet watch run --project "$PSScriptRoot\..\PomoTime.csproj"
