# EasyCPDLC Printer/eLC 1.1.0

This is the first release using the fork-owned version namespace. Future releases use tags such as `printer-elc-v1.1.0` instead of reusing upstream EasyCPDLC version tags.

## Versioning changes

- Git tag namespace: `printer-elc-vMAJOR.MINOR.PATCH`
- Windows file version: `MAJOR.MINOR.PATCH.0`
- in-app badge: `P/eLC MAJOR.MINOR.PATCH`
- fork-branded download names
- updater ignores upstream and unrelated unnamespaced tags
- legacy release `1.0.0.17` remains eligible as the migration baseline

Installed `1.0.0.17` copies will see `printer-elc-v1.1.0` as a normal upgrade because `1.1.0.0` sorts above `1.0.0.17`.

## Included software

The Windows x64 ZIP remains self-contained and includes the compiled optional vPilot bridge plus `Install-vPilot-Bridge.cmd`. Release users do not need the .NET SDK.

## Verification

- 57 automated tests passed
- Windows file version: `1.1.0.0`
- product version: `printer-elc-v1.1.0`
- ZIP SHA-256: `64866BF1AE5CA9F9DFDF3BCF2EABB6DC9363807FB9D31416C311AA42F45D3B38`

Flight simulation use only. This release is not for real-world aviation.
