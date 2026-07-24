# VNS430 MSFS 2024 keybind bridge

This folder is the simulator-side input path for the desktop VNS430 panel. It
lets cockpit hardware drive the panel; there is **no in-simulator 3D
instrument**. The desktop panel is the display.

It contains:

- the standalone WASM bridge (`Bridge/`);
- the VNS430 and DCDU MobiFlight profiles (`MobiFlight/`).

## How input flows

```text
physical button -> MobiFlight -> L:EASYCPDLC_VNS_COMMAND -> WASM bridge
    -> SimConnect Client Data (EasyCPDLC.VNS430.Command.v1) -> desktop panel
```

Status flows back the same way, so the module can light hardware annunciators:

- `L:EASYCPDLC_VNS_MODULE_ALIVE`
- `L:EASYCPDLC_VNS_APP_CONNECTED`
- `L:EASYCPDLC_VNS_VATSIM_CONNECTED`
- `L:EASYCPDLC_VNS_UNREAD_COUNT`
- `L:EASYCPDLC_VNS_PAGE`
- `L:EASYCPDLC_VNS_CURSOR_ACTIVE`
- `L:EASYCPDLC_DCDU_MODE`

The bridge maps no Garmin, GPS, radio, CDI, flight-plan, aircraft-key, or
InputEvent commands. It only carries the private `EASYCPDLC_VNS_*` /
`EASYCPDLC_DCDU_*` variables.

## Build the WASM bridge

With the MSFS 2024 SDK installed at its normal location:

```powershell
.\Bridge\Build-Wasm.ps1
```

The bridge uses named SimConnect Client Data:

```text
EasyCPDLC.VNS430.Command.v1
EasyCPDLC.VNS430.Status.v1
```

Install the built package (`Bridge/BuiltPackage/easycpdlc-vns430-bridge`) into
the MSFS Community folder.

## MobiFlight profiles

Import one of:

- `MobiFlight/EasyCPDLC-VNS430-Module.mfproj` for the VNS430 encoder and face
  buttons.
- `MobiFlight/EasyCPDLC-DCDU-Module.mfproj` for twelve DCDU LSKs plus connect,
  AOC, ATC, settings, reload, print, reprint, and hide.

Replace the placeholder controller binding with the real controller and pin
assignments.

VNS430 mode uses `L:EASYCPDLC_VNS_COMMAND` values 1 through 18. The tray option
**Use MSFS module for DCDU controls** gates the separate momentary
`L:EASYCPDLC_DCDU_*` inputs. VNS430 commands are ignored in DCDU mode, and DCDU
inputs are cleared and ignored outside DCDU mode.

## Using the panel as a screen

With hardware driving input, run the desktop panel as a bare LCD behind the
physical unit: right-click the tray icon and turn off **artwork** and
**interactive zones**. See the [VNS430 README](../README.md).

This module is not an aircraft ACARS adapter. Hoppie and future aircraft-inbox
routing remain documented in
[`docs/HOPPIE-AIRCRAFT-ACARS-ROUTING.md`](../../../docs/HOPPIE-AIRCRAFT-ACARS-ROUTING.md).
