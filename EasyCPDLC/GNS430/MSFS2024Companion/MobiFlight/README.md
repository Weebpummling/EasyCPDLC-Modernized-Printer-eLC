# MobiFlight companion-module profile

`EasyCPDLC-GNS430-Companion.mfproj` is for MobiFlight 11. Its 18 actions only write a number to the private companion-module input:

```text
N (>L:EASYCPDLC_GNS_COMMAND)
```

It contains no keyboard action, `GPS_*`, `AS430_*`, aircraft InputEvent, key event, FSUIPC write, radio action, or flight-plan action.

Open or merge the project, replace the placeholder controller in **Extras > Controller Bindings**, and then assign the two encoder rows and button rows to your actual hardware. MobiFlight cannot know a user's controller serial number or configured pin names in advance.

The EasyCPDLC MSFS 2024 companion WASM package must be installed and the GNS panel menu must show `MSFS MODULE: ACTIVE`. If it remains `WAITING`, the simulator connection exists but the companion heartbeat is not arriving.

For a physical Airbus/Boeing DCDU, import `EasyCPDLC-DCDU-Companion.mfproj`, replace its placeholder controller, and enable **Use MSFS companion for DCDU controls** in the EasyCPDLC tray. Its 20 actions cover all twelve LSKs plus connect, AOC, ATC, settings, flight-plan reload, print, reprint, and hide. For example, left LSK 1 uses `1 (>L:EASYCPDLC_DCDU_LSK_L1)` and ATC uses `1 (>L:EASYCPDLC_DCDU_ATC)`. The module clears each input after use. These inputs are inert unless DCDU mode is enabled, and the GNS profile's `EASYCPDLC_GNS_COMMAND` actions are inert while DCDU mode is enabled.
