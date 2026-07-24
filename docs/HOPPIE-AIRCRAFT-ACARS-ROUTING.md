# Hoppie and aircraft ACARS routing

## Decision

EasyCPDLC must be the only Hoppie network owner when an aircraft-ACARS bridge is enabled.

Two programs may be accepted by Hoppie when they use the same logon code and callsign, but that does not create two synchronized inboxes. Hoppie stores pending messages for the station and a `poll` request consumes them. Whichever program polls first receives a given message, producing a nondeterministic split between EasyCPDLC and the aircraft. A second logon code can instead trigger Hoppie's callsign lock.

The safe target topology is therefore:

```text
Hoppie <-> EasyCPDLC <-> supported aircraft adapter <-> aircraft ACARS/CDU
                    \\-> VNS430-style EasyCPDLC display
```

The aircraft's own Hoppie connection must be disabled while this mode is active. EasyCPDLC continues to send and poll once, then mirrors normalized inbound and outbound traffic to a supported aircraft adapter.

## User-facing mode requirements

The future **Aircraft ACARS bridge** option must be off by default and fail closed. It may become selectable only after a compatible aircraft and a working adapter are detected. Enabling it must require an explicit confirmation containing all of the following:

- EasyCPDLC will be the only Hoppie client for this callsign.
- Remove or disable the Hoppie code in the aircraft before continuing.
- Do not enable the mode if the aircraft is still connected directly to Hoppie.
- A failed or disconnected adapter leaves messages in EasyCPDLC; it must never silently discard them.

Changing aircraft, losing the simulator connection, or losing adapter capability must disable forwarding and show a visible status. The base EasyCPDLC inbox remains authoritative.

## PMDG 737 first adapter

The first target is the **PMDG 737 for MSFS 2024**. This specifically means the current native MSFS 2024 product, not the older MSFS 2020 PMDG 737. The 2024 aircraft exposes an ATC-network selection that can be set to `NONE`, allowing its stored Hoppie ID to remain inactive while EasyCPDLC owns the network connection.

The publicly documented PMDG simulator SDK provides aircraft data and control events. No supported public call for inserting an arbitrary ACARS/CPDLC message into the PMDG 737 inbox has been verified in this workspace.

Consequently, this repository does not drive CDU keys, write undocumented L-vars, scrape a display, or pretend that forwarding succeeded. Those approaches are fragile and could alter the flight-management system.

On the machine with MSFS 2024 and the PMDG SDK installed:

1. Install and load the PMDG 737 for MSFS 2024 on the SDK-equipped machine. Confirm the exact package/variant identifiers for the 600, 700, 800, 900, and any supported MAX variants.
2. Inspect the installed PMDG 737 SDK header, examples, package documentation, and exposed SimConnect client-data definitions for a supported datalink-provider or inbound-message interface. Do not assume the MSFS 2020 `PMDG_NG3_SDK.h` contract is unchanged.
3. With the aircraft's ATC network set to `NONE`, verify that it makes no Hoppie poll or send requests even if a Hoppie ID remains stored in the EFB.
4. Ask PMDG for the 737 datalink-provider integration contract if inbound message delivery is not part of the public simulator SDK.
5. Implement a PMDG 737 adapter only against that supported contract. Keep Hoppie credentials, sending, and polling in EasyCPDLC.
6. Map EasyCPDLC message identity, direction, sender, recipient, type, response options, and status into the format required by the PMDG 737. Preserve message IDs so acknowledgements cannot be duplicated.
7. Add aircraft/variant capability detection and the confirmation described above. Do not expose an enable switch when the adapter is unavailable.
8. Test inbound CPDLC, replies, free text, PDC/DCL, duplicate delivery, reconnect, callsign change, aircraft change, and adapter failure with the PMDG Hoppie network disabled.
9. Confirm with a controlled Hoppie test station that exactly one poller is active and each message appears in both EasyCPDLC displays and the PMDG 737 exactly once.

## Current package boundary

The packaged MSFS module is intentionally limited to private EasyCPDLC VNS430 controls, display, and status. It does not claim PMDG 737 ACARS compatibility. The release contains both importable MobiFlight profiles and the bridge source. When SDK-built packages exist under `EasyCPDLC/VNS430/MSFS2024Module/Bridge/BuiltPackage` and `EasyCPDLC/VNS430/MSFS2024Module/BuiltPackage`, the release builder includes their Community-package contents.

## Research references

- Hoppie ACARS server API: <https://www.hoppie.nl/acars/system/tech.html>
- Hoppie ACARS FAQ: <https://www.hoppie.nl/acars/system/faq.html>
- PMDG 737 for MSFS 2024 Hoppie/ATC-network behavior: <https://forum.pmdg.com/forum/main-forum/pmdg-737-for-msfs/general-discussion-no-support/380599-weather-request-via-ats-in-the-new-737-for-msfs2024>
- PMDG 737 SDK documentation location and MSFS 2020 header reference: <https://forum.pmdg.com/forum/main-forum/pmdg-737-for-msfs/general-discussion-no-support/222143-sdk-released>
