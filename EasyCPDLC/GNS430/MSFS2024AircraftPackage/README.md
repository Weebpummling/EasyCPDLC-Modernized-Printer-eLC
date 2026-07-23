# GNS 430 PMDG 737-800 aircraft package

This separate MSFS 2024 Community package does not edit PMDG or Microsoft
files. It overlays the stock `Asobo_MPA_GNS430` attachment on the PMDG
737-800's empty printer-panel DZU opening and replaces only its LCD with the
EasyCPDLC 240×128 display.

The stock GNS430 mesh, texture, bezel, knobs, and buttons remain intact. The
HTML gauge contains no replacement faceplate or button layer, so there is
nothing drawn over the original artwork.

## Build

```powershell
.\Build-Package.ps1
```

Output:

```text
BuiltPackage\zzzz-easycpdlc-pmdg-738-gns430
```

The builder copies each of the 12 installed PMDG preset
`attached_objects.cfg` files and appends the attachment:

```text
attachment_root = "SimAttachments/Instruments/Asobo_MPA_GNS430"
attachment_file = "model/GNS430.xml"
attach_offset = -0.1571,0.8220,13.5577
attach_pbh = -90.0000,0.0000,0.0000
vcockpit_parameter.0 = "VCockpit01_htmlgauge00_file,EasyCPDLC/DTL430/DTL430.html"
```

The position comes from the PMDG interior model's `Selcal_Dzu_Remove` world
transform. The -90-degree pitch aligns the attachment's front plane with the
horizontal pedestal surface. All six pose values remain build parameters for
small in-simulator calibration changes.

## Display path

```text
desktop GNS 240×128 renderer
  -> PNG snapshot
  -> EasyCPDLC.DTL430.Display.v1 CommBus event
  -> stock GNS430 LCD
```

The screen-only HTML does not issue `SimVar.SetSimVarValue`, key events,
Garmin events, CDI events, or radio commands. Because the stock model behavior
is retained, its physical controls retain the model's original animations and
simulator behavior.

## Verification

```powershell
.\Test-Package.ps1
```

The audit checks all 12 PMDG presets, the measured pose, the stock attachment
path, absence of a custom 3D model or HTML bezel/buttons, the LCD subscription,
and the standalone-module ABI.

Restart MSFS after installing or replacing either Community package; aircraft
attachments and standalone WASM modules are loaded when the package is mounted.
