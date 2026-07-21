[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$VPilotInstallDir = '',
    [string]$BridgeDllPath = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($VPilotInstallDir)) {
    $registry = Get-ItemProperty -LiteralPath 'HKCU:\Software\vPilot' -ErrorAction SilentlyContinue
    $VPilotInstallDir = [string]$registry.Install_Dir
}

if ([string]::IsNullOrWhiteSpace($VPilotInstallDir)) {
    $VPilotInstallDir = Join-Path $env:LOCALAPPDATA 'vPilot'
}

$pluginApi = Join-Path $VPilotInstallDir 'RossCarlson.Vatsim.Vpilot.Plugins.dll'
if (-not (Test-Path -LiteralPath $pluginApi)) {
    throw "vPilot plugin API was not found at '$pluginApi'. Install vPilot or pass -VPilotInstallDir."
}

if (Get-Process -Name 'vPilot' -ErrorAction SilentlyContinue) {
    throw 'Close vPilot before installing or replacing the EasyCPDLC bridge plugin.'
}

if (-not [string]::IsNullOrWhiteSpace($BridgeDllPath)) {
    $bridgeSource = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($BridgeDllPath)
}
else {
    $packagedCandidates = @(
        (Join-Path $PSScriptRoot 'Bridge\EasyCPDLC.VPilotBridge.dll'),
        (Join-Path $PSScriptRoot 'EasyCPDLC.VPilotBridge.dll')
    )
    $bridgeSource = $packagedCandidates |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($bridgeSource)) {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $project = Join-Path $repoRoot 'EasyCPDLC.VPilotBridge\EasyCPDLC.VPilotBridge.csproj'
    if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
        throw 'The release bridge DLL was not found. Download the complete EasyCPDLC release ZIP and extract it before running the installer.'
    }

    $dotnetCandidates = @(
        (Join-Path $env:USERPROFILE '.dotnet10\dotnet.exe'),
        (Join-Path $env:USERPROFILE '.dotnet\dotnet.exe')
    )
    $dotnet = $dotnetCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($dotnet)) {
        $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
        $dotnet = $dotnetCommand.Source
    }
    if ([string]::IsNullOrWhiteSpace($dotnet)) {
        throw '.NET SDK was not found. Release users should run this installer from the complete extracted release ZIP.'
    }

    & $dotnet build $project -c $Configuration "-p:VPilotInstallDir=$VPilotInstallDir"
    if ($LASTEXITCODE -ne 0) {
        throw 'The vPilot bridge build failed.'
    }

    $bridgeSource = Join-Path $repoRoot "EasyCPDLC.VPilotBridge\bin\$Configuration\net48\EasyCPDLC.VPilotBridge.dll"
}

if (-not (Test-Path -LiteralPath $bridgeSource -PathType Leaf)) {
    throw "The vPilot bridge DLL was not found at '$bridgeSource'."
}

$assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($bridgeSource)
if ($assemblyName.Name -ne 'EasyCPDLC.VPilotBridge') {
    throw "The selected file is not the EasyCPDLC vPilot bridge: '$bridgeSource'."
}

$plugins = Join-Path $VPilotInstallDir 'Plugins'
New-Item -ItemType Directory -Path $plugins -Force | Out-Null
$destination = Join-Path $plugins 'EasyCPDLC.VPilotBridge.dll'
Copy-Item -LiteralPath $bridgeSource -Destination $destination -Force
Unblock-File -LiteralPath $destination -ErrorAction SilentlyContinue

$sourceHash = (Get-FileHash -LiteralPath $bridgeSource -Algorithm SHA256).Hash
$installedHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
if ($sourceHash -ne $installedHash) {
    throw 'The installed vPilot bridge failed SHA-256 verification.'
}

Write-Host "Installed EasyCPDLC.VPilotBridge.dll to '$destination'."
Write-Host "SHA-256: $installedHash"
Write-Host 'Start vPilot, enter .debug, and confirm that EasyCPDLC vPilot PDC Bridge loaded.'
