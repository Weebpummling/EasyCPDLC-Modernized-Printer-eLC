[CmdletBinding()]
param(
    [string]$PmdgPackageRoot =
        'C:\Users\aal78\AppData\Roaming\Microsoft Flight Simulator 2024\Packages\Community\pmdg-aircraft-738',

    [string]$OutputRoot,

    # The interior node the attachment is anchored to. MSFS positions a
    # SIM_ATTACHMENT relative to a named node; attach_offset alone does not
    # place it. The PMDG 737-800 interior exposes no ATTACH_POINT_* nodes, so
    # this anchors to existing pedestal geometry instead.
    #
    # bl_Ped sits 0.113 m from the printer-panel DZU opening and survives to
    # LOD2. Selcal_Dzu_Remove is closer to the opening but exists only in LOD0,
    # so the anchor disappears as soon as the interior drops a LOD.
    [string]$AttachToNode = 'bl_Ped',

    # Offset from $AttachToNode to the printer-panel DZU opening, in metres.
    # Measured as Selcal_Dzu_Remove minus bl_Ped in 73X_VC_LOD0.gltf.
    # These are calibration values; expect to tune them in the simulator.
    [double]$OffsetX = 0.0420,
    [double]$OffsetY = -0.0040,
    [double]$OffsetZ = 0.1050,
    [double]$Pitch = -90.0,
    [double]$Bank = 0.0,
    [double]$Heading = 0.0,
    [double]$Scale = 1.0
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

# This package overrides PMDG's preset configs, so it must be derived from
# PMDG's own package. Building from our own output would compound overrides and
# bake a stale copy of somebody else's work into the result.
if ((Split-Path -Leaf ([IO.Path]::GetFullPath($PmdgPackageRoot))) -eq $packageName) {
    throw "PmdgPackageRoot points at this package's own output. Point it at pmdg-aircraft-738."
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

function Remove-Vns430Block {
    # Drops a previously appended EasyCPDLC block so rebuilding, or building
    # from a config that already carries one, cannot produce duplicates.
    param([string]$Text)

    $pattern = '(?ms)^\s*\[sim_attachment\.\d+\]' +
               '(?:(?!^\s*\[)[\s\S])*?alias\s*=\s*"?easycpdlc_vns430"?' +
               '(?:(?!^\s*\[)[\s\S])*'
    return [regex]::Replace($Text, $pattern, '', 'IgnoreCase')
}

function Get-ForeignAttachment {
    # attachment_root values that belong to neither PMDG nor EasyCPDLC. These
    # are other add-ons writing into PMDG's package; GSX injects
    # FSDT_Passengers_Seats this way.
    param([string]$Text)

    return [regex]::Matches($Text, '(?im)^\s*attachment_root\s*=\s*"?([^"\r\n]+)') |
        ForEach-Object { $_.Groups[1].Value.Trim() } |
        Where-Object { $_ -notmatch 'PMDG 737-800' -and $_ -notmatch 'Asobo_MPA_GNS430' }
}

function Add-Vns430Attachment {
    param([string]$SourceConfig, [string]$TargetConfig)

    $text = Remove-Vns430Block (Get-Content -LiteralPath $SourceConfig -Raw)
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
attach_to_node = "$AttachToNode"
attach_to_reference_point = ""
attach_offset = $offset
attach_pbh = $pbh
attach_scale = $(Format-Number $Scale)
always_execute_associate_js = 1
always_execute_model_behavior = 1
alias = "easycpdlc_vns430"
vcockpit_parameter.0 = "VCockpit01_htmlgauge00_file,EasyCPDLC/VNS430/VNS430.html"
"@

    New-Item -ItemType Directory -Path (Split-Path -Parent $TargetConfig) -Force | Out-Null
    Set-Utf8NoBom -Path $TargetConfig `
        -Value ($text.TrimEnd() + "`r`n`r`n" + $block.TrimStart() + "`r`n")
}

$presets = Get-ChildItem -LiteralPath $presetRoot -Directory |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'config\attached_objects.cfg') }
if ($presets.Count -ne 12) {
    throw "Expected 12 PMDG 737-800 presets, found $($presets.Count). Refusing a partial package."
}

# This package replaces PMDG's preset configs wholesale, so every block already
# in them is carried through verbatim, including ones other add-ons injected.
# Filtering those out would delete them from the simulator: GSX writes
# FSDT_Passengers_Seats directly into PMDG's package, and dropping it here
# would remove GSX's cabin seats from the affected presets.
#
# The cost of carrying them is that the result is a snapshot. Record what was
# copied so Validate-Package.ps1 can tell when the source has moved on and the
# package needs rebuilding.
$provenance = [ordered]@{}
$foreignSummary = @{}
foreach ($preset in $presets) {
    $sourceConfig = Join-Path $preset.FullName 'config\attached_objects.cfg'
    Add-Vns430Attachment `
        -SourceConfig $sourceConfig `
        -TargetConfig (Join-Path $resolvedOutputRoot (
            "SimObjects\Airplanes\PMDG 737-800\presets\pmdg\$($preset.Name)\config\attached_objects.cfg"))

    $sourceText = Get-Content -LiteralPath $sourceConfig -Raw
    $foreign = @(Get-ForeignAttachment $sourceText)
    $provenance[$preset.Name] = [ordered]@{
        source_sha256 = (Get-FileHash -LiteralPath $sourceConfig -Algorithm SHA256).Hash
        carried_foreign_attachments = $foreign
    }
    foreach ($item in $foreign) {
        $foreignSummary[$item] = 1 + $(if ($foreignSummary.ContainsKey($item)) { $foreignSummary[$item] } else { 0 })
    }
}

Set-Utf8NoBom -Path (Join-Path $resolvedOutputRoot 'easycpdlc-vns430-provenance.json') `
    -Value (([ordered]@{
        pmdg_package_root = [IO.Path]::GetFullPath($PmdgPackageRoot)
        presets = $provenance
    }) | ConvertTo-Json -Depth 6)

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
if ($foreignSummary.Count -eq 0) {
    Write-Host 'Carried no third-party attachments through from PMDG.'
}
else {
    Write-Host 'Carried these third-party attachments through from PMDG unchanged:'
    foreach ($item in ($foreignSummary.Keys | Sort-Object)) {
        Write-Host ("  {0} ({1} preset(s))" -f $item, $foreignSummary[$item])
    }
    Write-Host 'Rebuild this package if those add-ons are updated, moved, or removed.'
}
Write-Host ("Mounted in {0} presets on node '{1}' at offset {2},{3},{4} PBH {5},{6},{7} scale {8}" -f
    $presets.Count,
    $AttachToNode,
    (Format-Number $OffsetX),
    (Format-Number $OffsetY),
    (Format-Number $OffsetZ),
    (Format-Number $Pitch),
    (Format-Number $Bank),
    (Format-Number $Heading),
    (Format-Number $Scale))
