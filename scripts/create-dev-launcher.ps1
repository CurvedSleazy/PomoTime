$ErrorActionPreference = 'Stop'

$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }

$shortcutPath = Join-Path $repository 'PomoTime Dev.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $dotnet
$shortcut.Arguments = "watch run --project `"$repository\PomoTime.csproj`""
$shortcut.WorkingDirectory = $repository
$shortcut.Description = 'Run PomoTime with .NET Hot Reload'
$shortcut.WindowStyle = 1
$shortcut.Save()

Write-Host "Created trusted-host development launcher: $shortcutPath"
