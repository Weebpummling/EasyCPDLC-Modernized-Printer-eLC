<#
.SYNOPSIS
Rebuild and reinstall the VNS430 package when another add-on has changed
PMDG's preset configuration.

.DESCRIPTION
This package replaces PMDG's 12 preset attached_objects.cfg files, carrying
through any blocks other add-ons injected into them. GSX writes
FSDT_Passengers_Seats into PMDG's package that way, at install or update time
rather than per session.

That makes the installed package a snapshot. This script compares the snapshot
against the live PMDG configuration and, when they differ, rebuilds so the
other add-on's current work is picked up rather than overridden by a stale copy.

Run it after installing or updating GSX, PMDG, or anything else that writes to
the PMDG 737-800. It is safe to run at any time; with nothing to do it reports
and exits.

.EXAMPLE
.\Sync-Package.ps1
Rebuild and reinstall only if the live PMDG configuration has moved on.

.EXAMPLE
.\Sync-Package.ps1 -Force
Rebuild and reinstall regardless.

.EXAMPLE
.\Sync-Package.ps1 -WhatIf
Report what would change without touching anything.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$PmdgPackageRoot =
        'C:\Users\aal78\AppData\Roaming\Microsoft Flight Simulator 2024\Packages\Community\pmdg-aircraft-738',

    [string]$CommunityRoot =
        'C:\Users\aal78\AppData\Roaming\Microsoft Flight Simulator 2024\Packages\Community',

    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
$packageName = 'zzzz-easycpdlc-pmdg-738-vns430'
$installedRoot = Join-Path $CommunityRoot $packageName
$builtRoot = Join-Path $scriptRoot "BuiltPackage\$packageName"
$sourcePresetRoot = Join-Path $PmdgPackageRoot 'SimObjects\Airplanes\PMDG 737-800\presets\pmdg'

if (-not (Test-Path -LiteralPath $sourcePresetRoot -PathType Container)) {
    throw "PMDG preset root was not found: $sourcePresetRoot"
}

function Get-DriftReason {
    <#
        Returns the reasons the installed package no longer matches the live
        PMDG configuration. An empty result means it is current.
    #>
    param([string]$InstalledRoot, [string]$PresetRoot)

    $reasons = [Collections.Generic.List[string]]::new()
    if (-not (Test-Path -LiteralPath $InstalledRoot -PathType Container)) {
        $reasons.Add('The package is not installed in the Community folder.')
        return $reasons
    }

    $provenancePath = Join-Path $InstalledRoot 'easycpdlc-vns430-provenance.json'
    if (-not (Test-Path -LiteralPath $provenancePath -PathType Leaf)) {
        $reasons.Add('The installed package predates provenance tracking.')
        return $reasons
    }

    $provenance = Get-Content -LiteralPath $provenancePath -Raw | ConvertFrom-Json
    foreach ($entry in $provenance.presets.PSObject.Properties) {
        $live = Join-Path $PresetRoot "$($entry.Name)\config\attached_objects.cfg"
        if (-not (Test-Path -LiteralPath $live -PathType Leaf)) {
            $reasons.Add("Preset '$($entry.Name)' no longer exists in the PMDG package.")
            continue
        }
        if ((Get-FileHash -LiteralPath $live -Algorithm SHA256).Hash -ne $entry.Value.source_sha256) {
            $foreign = @($entry.Value.carried_foreign_attachments)
            $note = ''
            if ($foreign.Count -gt 0) { $note = " (carries $($foreign -join ', '))" }
            $reasons.Add("Preset '$($entry.Name)' has changed since the build$note.")
        }
    }

    # A preset that gained a config after the build is not in the provenance
    # record at all, so check for that separately.
    $liveNames = Get-ChildItem -LiteralPath $PresetRoot -Directory |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'config\attached_objects.cfg') } |
        ForEach-Object { $_.Name }
    $recorded = $provenance.presets.PSObject.Properties.Name
    foreach ($name in $liveNames) {
        if ($recorded -notcontains $name) {
            $reasons.Add("Preset '$name' is new since the build.")
        }
    }

    return $reasons
}

$reasons = Get-DriftReason -InstalledRoot $installedRoot -PresetRoot $sourcePresetRoot

if ($reasons.Count -eq 0 -and -not $Force) {
    Write-Host 'The installed VNS430 package matches the live PMDG configuration. Nothing to do.'
    Write-Host 'Use -Force to rebuild anyway.'
    return
}

if ($reasons.Count -gt 0) {
    Write-Host 'The installed VNS430 package is out of date:'
    foreach ($reason in $reasons) { Write-Host "  - $reason" }
}
else {
    Write-Host 'No drift detected; rebuilding because -Force was given.'
}

if (-not $PSCmdlet.ShouldProcess($installedRoot, 'Rebuild and reinstall the VNS430 package')) {
    Write-Host 'Stopping without changing anything.'
    return
}

Write-Host ''
& (Join-Path $scriptRoot 'Build-Package.ps1') -PmdgPackageRoot $PmdgPackageRoot
if ($LASTEXITCODE) { throw "Build-Package.ps1 failed with exit code $LASTEXITCODE" }

Write-Host ''
& (Join-Path $scriptRoot 'Validate-Package.ps1')
if ($LASTEXITCODE) { throw "Validate-Package.ps1 failed with exit code $LASTEXITCODE" }

# Only replace the installed copy once the new one has passed validation, so a
# failed build cannot leave the simulator without a working package.
Write-Host ''
if (Test-Path -LiteralPath $installedRoot) {
    Remove-Item -LiteralPath $installedRoot -Recurse -Force
}
Copy-Item -LiteralPath $builtRoot -Destination $installedRoot -Recurse -Force

Write-Host "Reinstalled: $installedRoot"
Write-Host 'Restart MSFS 2024 so the rebuilt package is picked up.'
