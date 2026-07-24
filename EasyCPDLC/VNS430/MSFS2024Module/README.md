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

## Co-existing with other add-ons

This package replaces PMDG's 12 preset `attached_objects.cfg` files rather than
adding to them; MSFS offers no additive mechanism, and the `zzzz-` prefix makes
this package load last and win.

Other add-ons write into those same files. GSX injects
`FSDT_Passengers_Seats` directly into PMDG's package, currently in 8 of the 12
presets. Every block already present is therefore carried through **verbatim**.
Filtering third-party blocks out would delete them from the simulator, which
for GSX means losing its cabin seats in those presets.

The consequence is that the built package is a point-in-time snapshot. The
build reports what it carried through, and records a SHA-256 of each source
config in `easycpdlc-vns430-provenance.json`. `Validate-Package.ps1` compares
those against the installed PMDG configs and fails when they no longer match,
so an add-on update is reported rather than silently overridden by a stale copy.

Rebuild this package after installing, updating, or removing any add-on that
writes to the PMDG 737-800, GSX included. To validate a package built on
another machine, pass `-SkipSourceDriftCheck`.

Rebuilding is safe to repeat: a previously appended EasyCPDLC block is removed
before a new one is added, so configs cannot accumulate duplicates, and the
builder refuses to run against its own output.

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
attach_to_node = "bl_Ped"
vcockpit_parameter.0 = "VCockpit01_htmlgauge00_file,EasyCPDLC/VNS430/VNS430.html"
```

## How the attachment is positioned

MSFS positions a `SIM_ATTACHMENT` **relative to a named interior node**.
`attach_offset` on its own does not place anything; without `attach_to_node`
the attachment does not appear in the cockpit at all.

The PMDG 737-800 interior exposes no `ATTACH_POINT_*` nodes, so the package
anchors to existing pedestal geometry. `attach_to_node` accepts any node name,
which is what PMDG's own configuration does for `AIRSTAIR_PANEL`.

The default anchor is `bl_Ped`, 0.113 m from the printer-panel DZU opening.
`Selcal_Dzu_Remove` sits closer to the opening but exists only in LOD0, so the
anchor vanishes as soon as the interior drops a LOD.

The anchor, all six pose values, and the scale are build parameters:

```powershell
.\Build-Package.ps1 -AttachToNode 'Rectangle462' -OffsetX 0.05 -OffsetZ 0.08
```

Offsets are node-relative metres and are expected to need in-simulator
calibration. Because they are small, a wrong sign moves the unit a few
centimetres rather than out of the aircraft, so it stays visible while tuning.
Note that the simulator's axis order and sign may not match the glTF's.

Validate the built package with:

```powershell
.\Validate-Package.ps1
```

Validation checks all 12 presets, the required stock attachment, the presence
of a named anchor node, that offsets are node-relative rather than absolute
model positions, absence of replacement 3D artwork, VNS430 LCD subscription,
and bridge ABI.

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
