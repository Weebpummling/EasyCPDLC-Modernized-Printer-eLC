# EasyCPDLC MSFS 2024 companion module

This is the simulator-side source for a standalone MSFS 2024 WASM module. It is not a Garmin unit and does not map, send, receive, or mask any `GPS_*`, `AS430_*`, B-var, key event, flight-plan event, radio event, or aircraft InputEvent.

## Data path

1. MobiFlight writes a number from 1 through 18 to `L:EASYCPDLC_GNS_COMMAND` using its normal WASM calculator-code support.
2. This module reads and immediately clears that private L-var.
3. It sends a versioned, checksummed packet through named SimConnect Client Data `EasyCPDLC.GNS430.Command.v1`.
4. The desktop EasyCPDLC panel validates the packet and passes the command to its existing UI executor.
5. EasyCPDLC returns connection, unread-count, page, and cursor status through `EasyCPDLC.GNS430.Status.v1`; the module exposes those values as private output L-vars for MobiFlight.

The module sends a command-zero heartbeat once per second. EasyCPDLC displays `MSFS MODULE: ACTIVE` only while that heartbeat is present.

## Release-package contents

The normal EasyCPDLC publish output includes `MobiFlight/EasyCPDLC-GNS430-Companion.mfproj`. The full release builder additionally creates a `Companion` folder containing that importable project, these SDK sources, and—when a real SDK-built `.wasm` is present under `BuiltPackage`—the Community-package contents.

This module is not an aircraft ACARS adapter. Hoppie and future aircraft-inbox routing are documented in `docs/HOPPIE-AIRCRAFT-ACARS-ROUTING.md`.

## Build requirement

The Microsoft Flight Simulator 2024 SDK is not installed on this development machine, so a `.wasm` binary cannot be produced or runtime-tested here yet. Do not hand-create the package/project XML: use the official SDK's **StandaloneModule** sample as the verified package shell.

1. Install the current MSFS 2024 SDK and its samples.
2. Copy `Samples/DevmodeProjects/Misc/StandaloneModule` to a new working folder.
3. In its `Sources/Code` project, replace the sample module source with `Sources/EasyCpdclCompanion.cpp` and add `Sources/EasyCpdclCompanionProtocol.h`.
4. Keep the MSFS 2024 platform toolset and `MSFS_WasmVersions.a` configuration supplied by the sample.
5. Change the module output name to `easycpdlc-companion.wasm`, build it, and let the official Project Editor generate the package metadata.
6. Copy the built package folder to the MSFS 2024 Community folder and restart the simulator.

The current SDK documents standalone modules as automatically loaded when their package is mounted. Using the official sample avoids brittle, hand-authored package XML and gives DevMode the correct `WasmModule` asset group.

## Commands

| Value | Command | Value | Command |
| ---: | --- | ---: | --- |
| 1 | Right large knob decrease | 10 | FPL |
| 2 | Right large knob increase | 11 | PROC |
| 3 | Right small knob decrease | 12 | Direct-to |
| 4 | Right small knob increase | 13 | OBS |
| 5 | Push cursor | 14 | CDI |
| 6 | ENT | 15 | Range in |
| 7 | CLR | 16 | Range out |
| 8 | MENU | 17 | Nearest |
| 9 | MSG | 18 | Power |

MobiFlight calculator-code example for MENU:

```text
8 (>L:EASYCPDLC_GNS_COMMAND)
```

## Output L-vars

- `L:EASYCPDLC_GNS_MODULE_ALIVE`
- `L:EASYCPDLC_GNS_APP_CONNECTED`
- `L:EASYCPDLC_GNS_VATSIM_CONNECTED`
- `L:EASYCPDLC_GNS_UNREAD_COUNT`
- `L:EASYCPDLC_GNS_PAGE`
- `L:EASYCPDLC_GNS_CURSOR_ACTIVE`
