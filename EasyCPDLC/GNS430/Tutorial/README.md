# GNS 430 datalink tutorial

This tutorial covers the experimental EasyCPDLC GNS 430 front end. It is for flight simulation only. The panel uses the normal EasyCPDLC backend, inbox, Hoppie session, SimBrief data, and eLoadControl integration.

## 1. Save the shared credentials

Right-click the EasyCPDLC tray icon and choose **Connection credentials...**. Save the VATSIM CID, Hoppie logon code, SimBrief username or pilot ID, and optional eLoadControl API key.

These values belong to the EasyCPDLC backend, not to one display. They remain available after switching between the Airbus DCDU, Boeing DCDU, and GNS 430 panel and after restarting EasyCPDLC. Protected codes are stored with Windows current-user protection.

Before connecting, set the aircraft's own Hoppie/ATC network to **NONE** and remove its Hoppie code. Do not let the aircraft and EasyCPDLC poll the same callsign simultaneously.

## 2. Open and recognize the unit

Choose **Open GNS 430 panel** from the tray menu, or start `EasyCPDLC.exe --gns430`.

![GNS 430 panel running a direct request](images/01-unit-in-use.png)

The display groups down the left are adapted to datalink work:

- `DLK`: status and connection pages.
- `ATC`: logon and ATC request pages.
- `AOC`: telex, weather, clearance, and load-control pages.
- `MSG`: inbox and message details.

The bottom LCD annunciators line up with the physical keys. Amber `MSG` means unread inbound traffic. `CRSR` appears when the cursor is active. These indications are drawn only inside the LCD.

## 3. Operate the photographed controls

The panel is mouse-only unless the private MSFS companion is installed:

- Hold the left mouse button on a photographed key and release over the same key to activate it. Dragging away cancels the press.
- Point at the center or middle ring of the right encoder and use the wheel for the small knob.
- Point at the outer ring and use the wheel for the large knob.
- Click and release the center of the right encoder to push `CRSR`.
- With the cursor off, the large knob changes the page group and the small knob changes the page within that group.
- With the cursor on, the large knob moves through fields and the small knob changes the selected value.

![Right-side photographed buttons and dual encoder](images/03-right-controls.png)

Useful shortcuts on the faceplate are `FPL` for ATC requests, `PROC` for AOC/telex, `MSG` for the inbox, `CDI` for the VATSIM connection, `ENT` to accept, and `CLR` to erase or return.

## 4. Build and review an ATC request

Press `FPL`, select a request type, then press the cursor and edit the fields. This direct-request example shows the GNS-style field frame, inverse selection, position indicator, review action, and LCD annunciator row:

![Direct request LCD](images/02-lcd-page.png)

For the page shown:

1. Use the large knob to select `RECIPIENT`, `WAYPOINT`, `DUE TO`, or `REMARKS`.
2. Use the small knob to change the selected character or option.
3. Press `ENT` on `ENT REVIEW`.
4. Check the review page, then press `ENT` again to send through the existing EasyCPDLC backend.
5. Use `CLR` to erase the active field or return without sending.

The same edit-review-send pattern is used for level, speed, when-can-we, free-text, AOC telex, METAR, ATIS, PDC, oceanic clearance, and eLoadControl workflows.

## 5. Read and answer messages

Press `MSG`. An unread inbound message opens first and is marked read when displayed; otherwise the full message list opens. Use the large knob to select a message, `ENT` to open it, the small knob to select an available response, and `ENT` to send that response. `CLR` returns to the list.

## 6. Use MobiFlight hardware

Install the SDK-built EasyCPDLC companion package, import `MSFS2024Companion/MobiFlight/EasyCPDLC-GNS430-Companion.mfproj`, replace its placeholder controller binding, and assign the actions to the real encoder and buttons. In the GNS menu, enable **MSFS MODULE**. `ACTIVE` confirms the heartbeat and desktop link.

For a physical Airbus/Boeing DCDU instead, import `EasyCPDLC-DCDU-Companion.mfproj`, right-click the tray, and enable **Use MSFS companion for DCDU controls**. In that mode the module accepts only the private `EASYCPDLC_DCDU_*` LSK/button variables and ignores `EASYCPDLC_GNS_COMMAND`. Disable the option to return the companion to GNS mode.

No EasyCPDLC companion action sends Garmin, aircraft GPS, flight-plan, radio, key, or InputEvent commands to MSFS.

## Current build limitation

The companion source and MobiFlight GNS profile are included. The complete GNS panel remains usable with the mouse when the companion is not installed.
