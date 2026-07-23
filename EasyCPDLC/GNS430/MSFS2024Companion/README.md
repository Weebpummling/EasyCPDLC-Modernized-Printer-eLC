# EasyCPDLC MSFS 2024 companion module

This is the simulator-side source for a standalone MSFS 2024 WASM module. It is not a Garmin unit and does not map, send, receive, or mask any `GPS_*`, `AS430_*`, B-var, key event, flight-plan event, radio event, or aircraft InputEvent.

## Data path

1. In GNS mode, MobiFlight writes a number from 1 through 18 to `L:EASYCPDLC_GNS_COMMAND` using its normal WASM calculator-code support. In the optional DCDU mode it pulses one of the private `L:EASYCPDLC_DCDU_*` inputs instead.
2. This module reads and immediately clears that private L-var.
3. It sends a versioned, checksummed packet through named SimConnect Client Data `EasyCPDLC.GNS430.Command.v1`.
4. The desktop EasyCPDLC panel validates the packet and passes the command to the selected GNS or DCDU UI executor.
5. EasyCPDLC returns connection, unread-count, page, and cursor status through `EasyCPDLC.GNS430.Status.v1`; the module exposes those values as private output L-vars for MobiFlight.

The module sends a command-zero heartbeat once per second. EasyCPDLC displays `MSFS MODULE: ACTIVE` only while that heartbeat is present. `L:EASYCPDLC_DCDU_MODE` reports the active gate. GNS commands are ignored while it is `1`; DCDU inputs are cleared and ignored while it is `0`.

## Release-package contents

The normal EasyCPDLC publish output includes the importable GNS and DCDU projects under `MobiFlight/`. The full release builder additionally creates a `Companion` folder containing both projects, these SDK sources, and—when a real SDK-built `.wasm` is present under `BuiltPackage`—the Community-package contents.

This module is not an aircraft ACARS adapter. Hoppie and future aircraft-inbox routing are documented in `docs/HOPPIE-AIRCRAFT-ACARS-ROUTING.md`.

## Build

The installed MSFS 2024 SDK toolchain at `C:\MSFS 2024 SDK` can build the
standalone module without Visual Studio:

```powershell
.\Build-Wasm.ps1
```

The script compiles with the SDK's WASI `clang-cl`, links
`MSFS_WasmVersions.a`, exports the standalone module entry points, validates
the WASM magic, and creates this ready-to-install package:

```text
BuiltPackage\easycpdlc-companion
```

Copy that directory into the MSFS 2024 Community folder and restart the
simulator. The package contains only `modules/easycpdlc-companion.wasm` plus
its manifest/layout metadata.

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
- `L:EASYCPDLC_DCDU_MODE`

## DCDU-only momentary input L-vars

Enable **Use MSFS companion for DCDU controls** from the EasyCPDLC tray before using these inputs. Write `1` on press; the module clears the value after consuming it.

- `L:EASYCPDLC_DCDU_LSK_L1` through `L:EASYCPDLC_DCDU_LSK_L6`
- `L:EASYCPDLC_DCDU_LSK_R1` through `L:EASYCPDLC_DCDU_LSK_R6`
- `L:EASYCPDLC_DCDU_CONNECT`
- `L:EASYCPDLC_DCDU_AOC`
- `L:EASYCPDLC_DCDU_ATC`
- `L:EASYCPDLC_DCDU_SETTINGS`
- `L:EASYCPDLC_DCDU_RELOAD`
- `L:EASYCPDLC_DCDU_PRINT`
- `L:EASYCPDLC_DCDU_REPRINT`
- `L:EASYCPDLC_DCDU_HIDE`

MobiFlight example for left LSK 1:

```text
1 (>L:EASYCPDLC_DCDU_LSK_L1)
```
