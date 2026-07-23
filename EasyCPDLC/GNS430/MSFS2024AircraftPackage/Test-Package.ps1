[CmdletBinding()]
param(
    [string]$PackageRoot =
        (Join-Path $PSScriptRoot 'BuiltPackage\zzzz-easycpdlc-pmdg-738-gns430'),
    [string]$CompanionPackageRoot =
        (Join-Path $PSScriptRoot '..\MSFS2024Companion\BuiltPackage\easycpdlc-companion')
)

$ErrorActionPreference = 'Stop'
$failures = [Collections.Generic.List[string]]::new()
function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { $script:failures.Add($Message) }
}

$gaugeRoot = Join-Path $PackageRoot 'html_ui\Pages\VCockpit\Instruments\EasyCPDLC\DTL430'
$htmlPath = Join-Path $gaugeRoot 'DTL430.html'
$cssPath = Join-Path $gaugeRoot 'DTL430.css'
$jsPath = Join-Path $gaugeRoot 'DTL430.js'
$presetRoot = Join-Path $PackageRoot 'SimObjects\Airplanes\PMDG 737-800\presets\pmdg'
$wasmPath = Join-Path $CompanionPackageRoot 'modules\easycpdlc-companion.wasm'

foreach ($path in @($htmlPath, $cssPath, $jsPath, $wasmPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "Missing required file: $path"
}
if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }

$html = Get-Content -LiteralPath $htmlPath -Raw
$js = Get-Content -LiteralPath $jsPath -Raw
Assert-True ($html -match '<canvas[^>]+width="240"[^>]+height="128"') `
    'The replacement LCD must retain the desktop renderer 240x128 resolution.'
Assert-True ($html -notmatch '(?i)<button|data-command|dtl430-face') `
    'The HTML gauge must not draw a replacement bezel or buttons.'
Assert-True ($js -match 'EasyCPDLC\.DTL430\.Display\.v1') `
    'The LCD does not subscribe to the EasyCPDLC display event.'
Assert-True ($js -notmatch '(?i)SetSimVarValue|triggerKeyEvent|sendKeyEvent|Coherent\.call') `
    'The screen-only gauge must not issue simulator, Garmin, CDI, or radio commands.'

$customModels = Get-ChildItem -LiteralPath $PackageRoot -Recurse -File |
    Where-Object { $_.Extension -in '.gltf', '.glb', '.bin', '.xml' -and
        $_.FullName -match 'attachments[\\/]easycpdlc' }
Assert-True ($customModels.Count -eq 0) `
    'The package contains a replacement 3D model; it must use the stock GNS430 attachment.'

$configs = Get-ChildItem -LiteralPath $presetRoot -Recurse -File -Filter attached_objects.cfg
Assert-True ($configs.Count -eq 12) "Expected 12 PMDG preset overrides; found $($configs.Count)."
foreach ($config in $configs) {
    $text = Get-Content -LiteralPath $config.FullName -Raw
    Assert-True ($text -match 'attachment_root\s*=\s*"SimAttachments/Instruments/Asobo_MPA_GNS430"') `
        "Stock GNS430 attachment root missing in $($config.FullName)"
    Assert-True ($text -match 'attachment_file\s*=\s*"model/GNS430\.xml"') `
        "Stock GNS430 model missing in $($config.FullName)"
    Assert-True ($text -match 'attach_offset\s*=\s*-0\.1571,0\.8220,13\.5577') `
        "Measured printer-panel position missing in $($config.FullName)"
    Assert-True ($text -match 'attach_pbh\s*=\s*-90\.0000,0\.0000,0\.0000') `
        "Printer-panel orientation missing in $($config.FullName)"
    Assert-True ($text -match 'vcockpit_parameter\.0\s*=\s*"VCockpit01_htmlgauge00_file,EasyCPDLC/DTL430/DTL430\.html"') `
        "LCD override missing in $($config.FullName)"
}

$wasm = [IO.File]::ReadAllBytes($wasmPath)
Assert-True ($wasm.Length -ge 8 -and $wasm[0] -eq 0 -and $wasm[1] -eq 0x61 -and
    $wasm[2] -eq 0x73 -and $wasm[3] -eq 0x6d) 'Companion output is not WebAssembly.'
$wasmText = [Text.Encoding]::UTF8.GetString($wasm)
Assert-True ($wasmText.Contains('register_named_variable')) `
    'The companion module does not use the compatible named-variable ABI.'
Assert-True (-not $wasmText.Contains('fsVarsRegisterLVar')) `
    'The companion module contains the standalone-host-incompatible fsVars ABI.'
Assert-True (-not $wasmText.Contains('__stack_chk_fail')) `
    'The companion module contains an unresolved stack-protector import.'
Assert-True ($wasmText.Contains('SimConnect_SubscribeToSystemEvent') -and
    $wasmText.Contains('1sec')) `
    'The companion module is not driven by the supported one-second system event.'
Assert-True ($wasmText.Contains('module_init') -and $wasmText.Contains('module_deinit')) `
    'The companion module lacks required standalone module entry points.'
Assert-True ($wasmText.Contains('__wasm_call_ctors')) `
    'The companion module lacks the SDK-required constructor export.'
Assert-True (-not $wasmText.Contains('module_update')) `
    'The companion module must use the subscribed 1sec event, not module_update.'

if ($failures.Count -gt 0) {
    throw ("GNS430 package audit failed:`r`n- " + ($failures -join "`r`n- "))
}
Write-Host 'PASS: 12 presets mount the stock GNS430 at the measured printer-panel pose.'
Write-Host 'PASS: EasyCPDLC replaces only the 240x128 LCD; no replacement bezel/buttons exist.'
Write-Host 'PASS: the companion module uses the MSFS standalone-compatible ABI.'
