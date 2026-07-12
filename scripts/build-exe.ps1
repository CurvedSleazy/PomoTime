$ErrorActionPreference = 'Stop'
$dotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
$outputDirectory = Join-Path $PSScriptRoot '..\publish'
$executable = Join-Path $outputDirectory 'PomoTime.exe'

& $dotnet publish "$PSScriptRoot\..\PomoTime.csproj" -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None `
    -p:Deterministic=true -p:ContinuousIntegrationBuild=true `
    -o $outputDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Enterprise-managed Windows devices can require an Authenticode signature.
# Set POMOTIME_SIGNING_THUMBPRINT to a trusted code-signing certificate that
# exists in Cert:\CurrentUser\My or Cert:\LocalMachine\My.
if ($env:POMOTIME_SIGNING_THUMBPRINT) {
    $thumbprint = $env:POMOTIME_SIGNING_THUMBPRINT.Replace(' ', '')
    $certificate = Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My -CodeSigningCert |
        Where-Object Thumbprint -eq $thumbprint |
        Select-Object -First 1
    if (-not $certificate) {
        throw "The configured code-signing certificate '$thumbprint' was not found or has no private key."
    }

    $signature = Set-AuthenticodeSignature -FilePath $executable -Certificate $certificate `
        -HashAlgorithm SHA256 -TimestampServer 'http://timestamp.digicert.com'
    if ($signature.Status -ne 'Valid') {
        throw "Authenticode signing failed: $($signature.StatusMessage)"
    }
    Write-Host "Signed with: $($certificate.Subject)"
}

$finalSignature = Get-AuthenticodeSignature $executable
if ($finalSignature.Status -ne 'Valid') {
    Write-Warning 'PomoTime.exe is unsigned. Windows Code Integrity policy may block it. Configure POMOTIME_SIGNING_THUMBPRINT with an enterprise-trusted code-signing certificate.'
}

Write-Host "Built: $executable"
