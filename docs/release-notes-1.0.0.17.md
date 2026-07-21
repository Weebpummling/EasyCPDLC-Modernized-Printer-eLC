# EasyCPDLC 1.0.0.17

This release packages the EasyCPDLC printer and eLoadControl fork as a self-contained Windows x64 application and includes the optional compiled vPilot/vTDLS bridge.

## Highlights

- printer dropdown remains open for normal mouse selection
- wide printer profile is labeled `GENERIC 4 INCH`
- SimBrief aliases save and fetch correctly
- eLoadControl API key can be entered from the Setup account page
- receipt dates are stable across Windows regional settings
- Boeing CONN/DISC interaction and VATSIM transition messaging are improved
- compiled `EasyCPDLC.VPilotBridge.dll` is bundled in the normal download
- double-click `Install-vPilot-Bridge.cmd` installs the optional bridge without requiring the .NET SDK

## Install

1. Download and extract `EasyCPDLC-1.0.0.17-win-x64.zip`.
2. Run `EasyCPDLC.exe`.
3. Optional vPilot bridge: close vPilot and double-click `Install-vPilot-Bridge.cmd`, then restart vPilot.

The main application is self-contained for Windows x64. The bridge installer copies the bundled DLL into the current user's vPilot `Plugins` folder.

## Verification

- 48 automated tests passed
- release build completed successfully
- packaged bridge installed locally and exposed `EasyCPDLC.VPilotBridge.v1` from vPilot
- ZIP SHA-256: `BDEB956AC9DD10A49486DADB78B668623E6848400C38E92680F7EF670265B1A7`
- bridge SHA-256: `50968D6632448A30040055AFB8603A197CCADE864A55801CD95B6BFE084D5093`

Flight simulation use only. This release is not for real-world aviation.
