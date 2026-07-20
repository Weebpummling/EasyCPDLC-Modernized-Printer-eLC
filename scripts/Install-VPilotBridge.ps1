[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$VPilotInstallDir = ''
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'EasyCPDLC.VPilotBridge\EasyCPDLC.VPilotBridge.csproj'

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
    throw '.NET SDK was not found.'
}

& $dotnet build $project -c $Configuration "-p:VPilotInstallDir=$VPilotInstallDir"
if ($LASTEXITCODE -ne 0) {
    throw 'The vPilot bridge build failed.'
}

$output = Join-Path $repoRoot "EasyCPDLC.VPilotBridge\bin\$Configuration\net48\EasyCPDLC.VPilotBridge.dll"
$plugins = Join-Path $VPilotInstallDir 'Plugins'
New-Item -ItemType Directory -Path $plugins -Force | Out-Null
Copy-Item -LiteralPath $output -Destination (Join-Path $plugins 'EasyCPDLC.VPilotBridge.dll') -Force

Write-Host "Installed EasyCPDLC.VPilotBridge.dll to '$plugins'."
Write-Host 'Start vPilot, enter .debug, and confirm that EasyCPDLC vPilot PDC Bridge loaded.'
