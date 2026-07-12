$ErrorActionPreference = 'Stop'
$dotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
& $dotnet publish "$PSScriptRoot\..\PomoTime.csproj" -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None `
    -o "$PSScriptRoot\..\publish"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Built: $PSScriptRoot\..\publish\PomoTime.exe"
