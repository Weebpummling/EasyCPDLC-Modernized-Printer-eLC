# EasyCPDLC GNS 430 panel

> **TESTING ONLY — NOT A SUPPORTED RELEASE.** This mod is an experimental simulator interface. Expect incomplete pages and integration changes while it is being tested.

This folder is an isolated front end for mixed-equipment home cockpits. It uses the existing `MainForm` connection, Hoppie/CPDLC session, message store, ATC request pages, AOC/telex pages, and settings. It does not copy or fork the network backend.

## Required Hoppie setup

Before connecting EasyCPDLC, set the aircraft's internal Hoppie/ATC network to **NONE** and remove or disable the Hoppie code stored in the aircraft. EasyCPDLC must be the only Hoppie client operating under the flight's callsign—even before the planned PMDG bridge is available.

Do not run EasyCPDLC and the aircraft's native Hoppie client together. Hoppie provides one pending-message queue for a station, not synchronized inboxes, so two pollers can divide messages unpredictably. EasyCPDLC remains the authoritative Hoppie inbox. The planned PMDG 737 adapter will mirror traffic from EasyCPDLC into the aircraft without creating another Hoppie connection.

The visual and interaction model follows the Garmin GNS 430 Pilot's Guide (190-00140-00 Rev. P), especially sections 1.2 and 1.3:

- Large right knob: change the `NAV`/`WPT`/`AUX`/`NRST` page group with the cursor off; move the cursor through fields or list items with it on.
- Small right knob: change the page within a group with the cursor off; change the selected value with it on.
- Push `CRSR`: enable or disable the cursor.
- `ENT`: accept or activate.
- `CLR`: erase, cancel, or return.
- `MSG`: message list.
- `FPL`: EasyCPDLC ATC request menu.
- `PROC`: EasyCPDLC AOC/telex menu.
- Direct-to: CPDLC direct-logon page.
- `RNG`: display text size.
- `CDI`: connect or disconnect VATSIM.

The LCD is rendered at the real unit's native `240x128` raster resolution and enlarged with nearest-neighbor scaling. It uses a purpose-built 5x7 bitmap alphabet instead of WinForms font rendering, plus the palette sampled from every display capture in the Pilot's Guide: `#3853A4` blue, `#040707` black, `#6ECDDD` cyan, `#69BD45` green, `#F3EC19` yellow, `#B9519F` magenta, and white. Fields, menus, inverse-video selections, scrollbars, page groups, page squares, and bottom annunciators are drawn using the same pixel-coordinate grammar as the guide.

The external panel is photographic artwork rather than a painted WinForms approximation. `Assets/panel-background.png` is a perspective-corrected front-panel crop, and `Assets/controls/` contains separate normal, pressed, range-rocker, pushed-knob, small-ring rotation, and large-ring rotation states. See `Assets/SOURCE.md` for source and personal-use provenance.

## Pilot Guide screen reference library

`scripts/Extract-Gns430ManualAssets.py` scans the complete Pilot's Guide and extracts every native `240x128` display image. The generated reference set under `output/gns430-reference/` contains:

- 479 UI image instances in every screen-sized resolution used by the guide, including 444 native `240x128` captures.
- A JSON and CSV page/caption manifest.
- The 64 most common exact display colors.
- Numbered contact sheets for visual comparison across every page, menu, popup, cursor state, and page group.

This reference set is design input and is not embedded into the application. The application recreates the screen grammar for EasyCPDLC data rather than displaying Garmin screenshots as operational pages.

## Opening it

Right-click the EasyCPDLC tray icon and choose **Open GNS 430 panel**. A shortcut may also start the normal executable with:

```text
EasyCPDLC.exe --gns430
```

Closing the panel hides it; the EasyCPDLC backend remains active in the tray.

## Mouse and keyboard

Click the left or right half of the outer right-knob ring for large-knob decrease/increase. Click the left or right half of the middle ring for small-knob decrease/increase. Click the center for `PUSH CRSR`. The left COM/VLOC controls are deliberately not mouse-mapped to the GPS page knob, matching the separation of controls on the physical unit.

When the panel has keyboard focus:

| Action | Key |
| --- | --- |
| Large knob decrease/increase | Left / Right |
| Small knob decrease/increase | Down / Up |
| Push cursor | Space |
| ENT / CLR | Enter / Escape or Backspace |
| MENU / MSG / FPL / PROC | M / G / F / P |
| Direct-to / OBS | D / O |
| Range in/out | Plus / Minus |

There are no global EasyCPDLC shortcuts. Keyboard navigation is accepted only while the EasyCPDLC panel has focus.

## MSFS 2024 companion module

The simulator-facing path is a private standalone WASM companion, documented under [`MSFS2024Companion`](MSFS2024Companion/README.md). It does not impersonate a Garmin unit and never registers, receives, emits, or masks Garmin/GPS aircraft events.

MobiFlight writes command numbers to `L:EASYCPDLC_GNS_COMMAND`. The companion validates the value, clears it, and passes a checksummed command packet through named SimConnect Client Data. EasyCPDLC returns module status, VATSIM connection state, unread count, current page, and cursor state for MobiFlight output devices.

Use **MSFS MODULE** in the panel menu. `WAITING` means SimConnect is open but the standalone module heartbeat has not arrived; `ACTIVE` means the complete module path is working. The ready-made MobiFlight 11 project is [`EasyCPDLC-GNS430-Companion.mfproj`](MSFS2024Companion/MobiFlight/EasyCPDLC-GNS430-Companion.mfproj).

## Current boundary and next integration step

Message browsing, CPDLC replies, VATSIM connect/disconnect, and direct logon are native to the GNS panel. Complex ATC request, AOC/telex, and settings editors open the existing EasyCPDLC pages so they continue using their mature validation and send logic.

The companion protocol is intentionally aircraft-independent. Do not add aircraft-specific GNS events to it; extend the versioned `EasyCPDLC.GNS430.*` client-data protocol or private `EASYCPDLC_GNS_*` L-vars instead.

The first planned aircraft-inbox adapter is the **PMDG 737 for MSFS 2024**. It is not implemented in this testing branch: a supported PMDG datalink message-ingress interface must first be confirmed on the SDK-equipped machine. See [`docs/HOPPIE-AIRCRAFT-ACARS-ROUTING.md`](../../docs/HOPPIE-AIRCRAFT-ACARS-ROUTING.md) for the implementation and validation plan.

This panel is for simulator use only and is not approved for real-world navigation.
