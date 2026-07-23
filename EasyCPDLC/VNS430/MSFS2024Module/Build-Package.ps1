[CmdletBinding()]
param(
    [string]$PmdgPackageRoot =
        'C:\Users\aal78\AppData\Roaming\Microsoft Flight Simulator 2024\Packages\Community\pmdg-aircraft-738',

    [string]$OutputRoot,

    # Measured from the PMDG interior model's Selcal_Dzu_Remove node.
    [double]$OffsetX = -0.1571,
    [double]$OffsetY = 0.8220,
    [double]$OffsetZ = 13.5577,
    [double]$Pitch = -90.0,
    [double]$Bank = 0.0,
    [double]$Heading = 0.0
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
$packageName = 'zzzz-easycpdlc-pmdg-738-vns430'
$sourceRoot = Join-Path $scriptRoot 'PackageSources'
$manifestTemplate = Join-Path $scriptRoot 'manifest.template.json'
$utf8NoBom = [Text.UTF8Encoding]::new($false)

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $scriptRoot "BuiltPackage\$packageName"
}

$presetRoot = Join-Path $PmdgPackageRoot 'SimObjects\Airplanes\PMDG 737-800\presets\pmdg'
if (-not (Test-Path -LiteralPath $presetRoot -PathType Container)) {
    throw "PMDG preset root was not found: $presetRoot"
}

$resolvedOutputRoot = [IO.Path]::GetFullPath($OutputRoot)
$builtRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot 'BuiltPackage'))
if (-not $resolvedOutputRoot.StartsWith($builtRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "For safety, OutputRoot must stay inside $builtRoot"
}

if (Test-Path -LiteralPath $resolvedOutputRoot) {
    Remove-Item -LiteralPath $resolvedOutputRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $resolvedOutputRoot | Out-Null
Copy-Item -Path (Join-Path $sourceRoot '*') -Destination $resolvedOutputRoot -Recurse -Force

function Set-Utf8NoBom {
    param([string]$Path, [string]$Value)
    [IO.File]::WriteAllText($Path, $Value, $script:utf8NoBom)
}

function Format-Number {
    param([double]$Value)
    $Value.ToString('0.0000', [Globalization.CultureInfo]::InvariantCulture)
}

function Add-Vns430Attachment {
    param([string]$SourceConfig, [string]$TargetConfig)

    $text = Get-Content -LiteralPath $SourceConfig -Raw
    $indices = [regex]::Matches($text, '(?im)^\s*\[SIM_ATTACHMENT\.(\d+)\]\s*$') |
        ForEach-Object { [int]$_.Groups[1].Value }
    $nextIndex = if ($indices.Count -eq 0) { 0 } else {
        ($indices | Measure-Object -Maximum).Maximum + 1
    }
    $offset = "$(Format-Number $OffsetX),$(Format-Number $OffsetY),$(Format-Number $OffsetZ)"
    $pbh = "$(Format-Number $Pitch),$(Format-Number $Bank),$(Format-Number $Heading)"
    $block = @"

[SIM_ATTACHMENT.$nextIndex]
attachment_root = "SimAttachments/Instruments/Asobo_MPA_GNS430"
attachment_file = "model/GNS430.xml"
attach_to_model = "interior"
attach_to_model_minsize = 0
attach_offset = $offset
attach_pbh = $pbh
always_execute_associate_js = 1
always_execute_model_behavior = 1
alias = "easycpdlc_vns430"
vcockpit_parameter.0 = "VCockpit01_htmlgauge00_file,EasyCPDLC/VNS430/VNS430.html"
"@

    New-Item -ItemType Directory -Path (Split-Path -Parent $TargetConfig) -Force | Out-Null
    Set-Utf8NoBom -Path $TargetConfig `
        -Value ($text.TrimEnd() + "`r`n" + $block.TrimStart() + "`r`n")
}

$presets = Get-ChildItem -LiteralPath $presetRoot -Directory |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'config\attached_objects.cfg') }
if ($presets.Count -ne 12) {
    throw "Expected 12 PMDG 737-800 presets, found $($presets.Count). Refusing a partial package."
}

foreach ($preset in $presets) {
    Add-Vns430Attachment `
        -SourceConfig (Join-Path $preset.FullName 'config\attached_objects.cfg') `
        -TargetConfig (Join-Path $resolvedOutputRoot (
            "SimObjects\Airplanes\PMDG 737-800\presets\pmdg\$($preset.Name)\config\attached_objects.cfg"))
}

$manifest = Get-Content -LiteralPath $manifestTemplate -Raw | ConvertFrom-Json
$pmdgManifestPath = Join-Path $PmdgPackageRoot 'manifest.json'
if (Test-Path -LiteralPath $pmdgManifestPath) {
    $pmdgManifest = Get-Content -LiteralPath $pmdgManifestPath -Raw | ConvertFrom-Json
    $manifest.dependencies[0].package_version = $pmdgManifest.package_version
}
$manifest.title = 'EasyCPDLC VNS430 for PMDG 737-800'
$manifest.package_version = '0.3.0'
$manifest.release_notes.neutral.LastUpdate =
    'Stock GNS430 model mounted over the printer-panel DZU opening; VNS430 replaces only the LCD.'
Set-Utf8NoBom -Path (Join-Path $resolvedOutputRoot 'manifest.json') `
    -Value ($manifest | ConvertTo-Json -Depth 10)

$files = Get-ChildItem -LiteralPath $resolvedOutputRoot -Recurse -File |
    Where-Object { $_.Name -ne 'layout.json' } |
    Sort-Object FullName
$layoutItems = foreach ($file in $files) {
    [ordered]@{
        path = $file.FullName.Substring($resolvedOutputRoot.Length + 1).Replace('\', '/')
        size = $file.Length
        date = $file.LastWriteTimeUtc.ToFileTimeUtc()
    }
}
Set-Utf8NoBom -Path (Join-Path $resolvedOutputRoot 'layout.json') `
    -Value ([ordered]@{ content = @($layoutItems) } | ConvertTo-Json -Depth 8)

Write-Host "Built VNS430 package with the stock GNS430 model: $resolvedOutputRoot"
Write-Host ("Mounted in {0} presets at {1},{2},{3} PBH {4},{5},{6}" -f
    $presets.Count,
    (Format-Number $OffsetX),
    (Format-Number $OffsetY),
    (Format-Number $OffsetZ),
    (Format-Number $Pitch),
    (Format-Number $Bank),
    (Format-Number $Heading))
