# Fork versioning

This fork uses its own namespaced semantic-versioning scheme so its releases cannot be confused with or collide with upstream EasyCPDLC releases.

## Release identity

- Git tag: `printer-elc-vMAJOR.MINOR.PATCH`
- Release title: `EasyCPDLC Printer/eLC MAJOR.MINOR.PATCH`
- Windows assembly/file version: `MAJOR.MINOR.PATCH.0`
- Download: `EasyCPDLC-Printer-eLC-MAJOR.MINOR.PATCH-win-x64.zip`
- In-app badge: `P/eLC MAJOR.MINOR.PATCH`

The first namespaced release is `printer-elc-v1.1.0`. The prior `1.0.0.17` release is retained as the one-time migration baseline so installed copies can upgrade normally.

## Incrementing versions

- `PATCH`: compatible fixes and packaging corrections
- `MINOR`: new printer, eLoadControl, vPilot bridge, or DCDU features
- `MAJOR`: incompatible configuration, protocol, or workflow changes

Only releases tagged with the `printer-elc-v` prefix are considered by the updater after the migration baseline. Tags from upstream or unrelated numeric tags in this fork are ignored.
