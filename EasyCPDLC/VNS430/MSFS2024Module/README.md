# VNS430 optional MSFS 2024 module

This folder is the single simulator-side component beneath VNS430. It contains
the optional PMDG 737-800 3D installation, the private WASM bridge, and both
importable MobiFlight profiles.

The prioritized SDK-machine work is recorded in
[`HANDOFF.md`](HANDOFF.md).

The module does not modify PMDG or Microsoft source files. The aircraft package
mounts MSFS's stock `Asobo_MPA_GNS430` attachment over the PMDG 737-800
printer-panel DZU opening and replaces only the LCD with VNS430's 240×128
display. `GNS430` in those two attachment identifiers refers to the actual
stock simulator model; all EasyCPDLC-owned names and protocols use `VNS430`.

## Known load limitation

The 3D attachment is optional and does not load reliably in every aircraft
preset or simulator session. MSFS caches mounted packages, aircraft
attachments, HTML gauges, and standalone WASM modules. After installing,
replacing, or rebuilding either package:

1. Close the aircraft session.
2. Restart MSFS 2024.
3. Confirm both Community packages are mounted.
4. Load a supported PMDG 737-800 preset.
5. Use the desktop VNS430 when the 3D attachment or LCD does not appear.

The desktop panel and shared EasyCPDLC backend do not depend on the 3D package.

## Build the 3D aircraft package

Run:

```powershell
.\Build-Package.ps1
```

The builder reads the 12 installed PMDG 737-800 preset configurations and
creates:

```text
BuiltPackage\zzzz-easycpdlc-pmdg-738-vns430
```

The required stock attachment remains:

```text
attachment_root = "SimAttachments/Instruments/Asobo_MPA_GNS430"
attachment_file = "model/GNS430.xml"
vcockpit_parameter.0 = "VCockpit01_htmlgauge00_file,EasyCPDLC/VNS430/VNS430.html"
```

The measured default position uses the PMDG interior model's
`Selcal_Dzu_Remove` transform. All six pose values remain build parameters for
in-simulator calibration.

Validate the built package with:

```powershell
.\Validate-Package.ps1
```

Validation checks all 12 presets, the measured pose, required stock attachment,
absence of replacement 3D artwork, VNS430 LCD subscription, and bridge ABI.

## Build the private WASM bridge

With the MSFS 2024 SDK installed at its normal location:

```powershell
.\Bridge\Build-Wasm.ps1
```

The bridge uses named SimConnect Client Data:

```text
EasyCPDLC.VNS430.Command.v1
EasyCPDLC.VNS430.Status.v1
EasyCPDLC.VNS430.Display.v1
```

It does not map or emit Garmin, GPS, radio, CDI, flight-plan, aircraft key, or
InputEvent commands.

## MobiFlight profiles

Import one of:

- `MobiFlight/EasyCPDLC-VNS430-Module.mfproj` for the VNS430 encoder and face
  buttons.
- `MobiFlight/EasyCPDLC-DCDU-Module.mfproj` for twelve DCDU LSKs plus
  connect, AOC, ATC, settings, reload, print, reprint, and hide.

Replace the placeholder controller binding with the real controller and pin
assignments.

VNS430 mode uses `L:EASYCPDLC_VNS_COMMAND` values 1 through 18. The tray option
**Use MSFS module for DCDU controls** gates the separate momentary
`L:EASYCPDLC_DCDU_*` inputs. VNS430 commands are ignored in DCDU mode, and DCDU
inputs are cleared and ignored outside DCDU mode.

Module output variables are:

- `L:EASYCPDLC_VNS_MODULE_ALIVE`
- `L:EASYCPDLC_VNS_APP_CONNECTED`
- `L:EASYCPDLC_VNS_VATSIM_CONNECTED`
- `L:EASYCPDLC_VNS_UNREAD_COUNT`
- `L:EASYCPDLC_VNS_PAGE`
- `L:EASYCPDLC_VNS_CURSOR_ACTIVE`
- `L:EASYCPDLC_DCDU_MODE`

This module is not an aircraft ACARS adapter. Hoppie and future aircraft-inbox
routing remain documented in
[`docs/HOPPIE-AIRCRAFT-ACARS-ROUTING.md`](../../../docs/HOPPIE-AIRCRAFT-ACARS-ROUTING.md).
