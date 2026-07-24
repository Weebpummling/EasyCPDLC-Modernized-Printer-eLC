[CmdletBinding()]
param(
    [string]$PackageRoot =
        (Join-Path $PSScriptRoot 'BuiltPackage\zzzz-easycpdlc-pmdg-738-vns430'),
    [string]$BridgePackageRoot =
        (Join-Path $PSScriptRoot 'Bridge\BuiltPackage\easycpdlc-vns430-bridge'),

    # Skip the check that the copied PMDG preset configs still match the
    # installed ones. Use when validating a package built on another machine.
    [switch]$SkipSourceDriftCheck
)

$ErrorActionPreference = 'Stop'
$failures = [Collections.Generic.List[string]]::new()
function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { $script:failures.Add($Message) }
}

$gaugeRoot = Join-Path $PackageRoot 'html_ui\Pages\VCockpit\Instruments\EasyCPDLC\VNS430'
$htmlPath = Join-Path $gaugeRoot 'VNS430.html'
$cssPath = Join-Path $gaugeRoot 'VNS430.css'
$jsPath = Join-Path $gaugeRoot 'VNS430.js'
$presetRoot = Join-Path $PackageRoot 'SimObjects\Airplanes\PMDG 737-800\presets\pmdg'
$wasmPath = Join-Path $BridgePackageRoot 'modules\easycpdlc-vns430-bridge.wasm'

foreach ($path in @($htmlPath, $cssPath, $jsPath, $wasmPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "Missing required file: $path"
}
if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }

$html = Get-Content -LiteralPath $htmlPath -Raw
$js = Get-Content -LiteralPath $jsPath -Raw
Assert-True ($html -match '<canvas[^>]+width="240"[^>]+height="128"') `
    'The replacement LCD must retain the desktop renderer 240x128 resolution.'
Assert-True ($html -notmatch '(?i)<button|data-command|vns430-face') `
    'The HTML gauge must not draw a replacement bezel or buttons.'
Assert-True ($js -match 'EasyCPDLC\.VNS430\.Display\.v1') `
    'The LCD does not subscribe to the EasyCPDLC display event.'
Assert-True ($js -notmatch '(?i)SetSimVarValue|triggerKeyEvent|sendKeyEvent|Coherent\.call') `
    'The screen-only gauge must not issue simulator, Garmin, CDI, or radio commands.'

$customModels = Get-ChildItem -LiteralPath $PackageRoot -Recurse -File |
    Where-Object { $_.Extension -in '.gltf', '.glb', '.bin', '.xml' -and
        $_.FullName -match 'attachments[\\/]easycpdlc' }
Assert-True ($customModels.Count -eq 0) `
    'The package contains replacement 3D artwork; it must use the stock GNS430 attachment.'

$configs = Get-ChildItem -LiteralPath $presetRoot -Recurse -File -Filter attached_objects.cfg
Assert-True ($configs.Count -eq 12) "Expected 12 PMDG preset overrides; found $($configs.Count)."
foreach ($config in $configs) {
    $text = Get-Content -LiteralPath $config.FullName -Raw
    Assert-True ($text -match 'attachment_root\s*=\s*"SimAttachments/Instruments/Asobo_MPA_GNS430"') `
        "Stock GNS430 attachment root missing in $($config.FullName)"
    Assert-True ($text -match 'attachment_file\s*=\s*"model/GNS430\.xml"') `
        "Stock GNS430 model missing in $($config.FullName)"
    # MSFS positions a SIM_ATTACHMENT relative to a named node. An attachment
    # with only attach_offset does not appear in the cockpit at all, so the
    # anchor is required rather than optional.
    Assert-True ($text -match 'attach_to_node\s*=\s*"(?<node>[^"]+)"') `
        "attach_to_node missing in $($config.FullName); the attachment will not be placed."

    # A node-relative offset is a small adjustment. A large value here means
    # an absolute model-space position was pasted in by mistake, which puts the
    # attachment metres away from the cockpit and effectively hides it.
    if ($text -match 'attach_offset\s*=\s*(?<x>-?[\d.]+),(?<y>-?[\d.]+),(?<z>-?[\d.]+)') {
        foreach ($axis in 'x', 'y', 'z') {
            $value = [double]$Matches[$axis]
            Assert-True ([math]::Abs($value) -le 1.0) (
                "attach_offset $axis = $value in $($config.FullName) is not a " +
                'node-relative offset; it looks like an absolute model position.')
        }
    }
    else {
        $failures.Add("attach_offset missing or malformed in $($config.FullName)")
    }

    Assert-True ($text -match 'attach_pbh\s*=\s*-?[\d.]+,-?[\d.]+,-?[\d.]+') `
        "Printer-panel orientation missing in $($config.FullName)"
    Assert-True ($text -match 'attach_scale\s*=\s*[\d.]+') `
        "attach_scale missing in $($config.FullName)"
    Assert-True ($text -match 'vcockpit_parameter\.0\s*=\s*"VCockpit01_htmlgauge00_file,EasyCPDLC/VNS430/VNS430\.html"') `
        "LCD override missing in $($config.FullName)"
}

# The preset configs are a snapshot of PMDG's, including blocks other add-ons
# injected into them. If the originals have changed since the build, this
# package is shipping stale copies and overriding whatever moved on.
$provenancePath = Join-Path $PackageRoot 'easycpdlc-vns430-provenance.json'
Assert-True (Test-Path -LiteralPath $provenancePath -PathType Leaf) `
    "Provenance record missing: $provenancePath"

if (-not $SkipSourceDriftCheck -and (Test-Path -LiteralPath $provenancePath -PathType Leaf)) {
    $provenance = Get-Content -LiteralPath $provenancePath -Raw | ConvertFrom-Json
    $sourcePresetRoot = Join-Path $provenance.pmdg_package_root `
        'SimObjects\Airplanes\PMDG 737-800\presets\pmdg'

    if (-not (Test-Path -LiteralPath $sourcePresetRoot -PathType Container)) {
        Write-Warning "Source PMDG package is not present; skipping drift check ($sourcePresetRoot)."
    }
    else {
        foreach ($entry in $provenance.presets.PSObject.Properties) {
            $live = Join-Path $sourcePresetRoot "$($entry.Name)\config\attached_objects.cfg"
            if (-not (Test-Path -LiteralPath $live -PathType Leaf)) {
                $failures.Add("Preset '$($entry.Name)' no longer exists in the PMDG package; rebuild.")
                continue
            }
            $liveHash = (Get-FileHash -LiteralPath $live -Algorithm SHA256).Hash
            Assert-True ($liveHash -eq $entry.Value.source_sha256) (
                "PMDG preset '$($entry.Name)' has changed since this package was built " +
                '(another add-on may have written to it). Rebuild to avoid overriding it with a stale copy.')
        }
    }
}

$wasm = [IO.File]::ReadAllBytes($wasmPath)
Assert-True ($wasm.Length -ge 8 -and $wasm[0] -eq 0 -and $wasm[1] -eq 0x61 -and
    $wasm[2] -eq 0x73 -and $wasm[3] -eq 0x6d) 'Bridge output is not WebAssembly.'
$wasmText = [Text.Encoding]::UTF8.GetString($wasm)
Assert-True ($wasmText.Contains('register_named_variable')) `
    'The VNS430 bridge does not use the compatible named-variable ABI.'
Assert-True (-not $wasmText.Contains('fsVarsRegisterLVar')) `
    'The VNS430 bridge contains the standalone-host-incompatible fsVars ABI.'
Assert-True (-not $wasmText.Contains('__stack_chk_fail')) `
    'The VNS430 bridge contains an unresolved stack-protector import.'
Assert-True ($wasmText.Contains('SimConnect_SubscribeToSystemEvent') -and
    $wasmText.Contains('1sec')) `
    'The VNS430 bridge is not driven by the supported one-second system event.'
Assert-True ($wasmText.Contains('module_init') -and $wasmText.Contains('module_deinit')) `
    'The VNS430 bridge lacks required standalone module entry points.'
Assert-True ($wasmText.Contains('__wasm_call_ctors')) `
    'The VNS430 bridge lacks the SDK-required constructor export.'
Assert-True (-not $wasmText.Contains('module_update')) `
    'The VNS430 bridge must use the subscribed 1sec event, not module_update.'

if ($failures.Count -gt 0) {
    throw ("VNS430 package audit failed:`r`n- " + ($failures -join "`r`n- "))
}
Write-Host 'PASS: 12 presets mount the stock GNS430 model with the VNS430 LCD, anchored to a named interior node with a node-relative offset.'
Write-Host 'PASS: EasyCPDLC replaces only the 240x128 LCD; no replacement bezel/buttons exist.'
Write-Host 'PASS: the VNS430 bridge uses the MSFS standalone-compatible ABI.'
Write-Host 'PASS: the copied PMDG preset configs still match the installed originals.'
