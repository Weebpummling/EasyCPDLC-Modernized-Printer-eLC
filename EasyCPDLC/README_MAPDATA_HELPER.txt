EasyCPDLC v82TUPDATE17I - logon version direct draw fallback

Logon version:
- Removed the overlay control again.
- No label, no child control, no floating form, no image-copy, no parent paint.
- Draws the version text directly onto the visible logon form/child control device context after the login UI has painted.
- Uses a short timer while the modal login window is open so the text stays visible after repaints.
- Positioned near the lower secure-connection area.

Previous changes preserved:
- Fixed user.config folder: %LOCALAPPDATA%\EasyCPDLCModernized\user.config
- Updater without .bak files
- Polished updater progress UI
- DPI-stable UI
- VATSIM map_data CPDLC helper
- No VATGlasses


Logon version tweak (v82TUPDATE17J):
- moved version text a bit upward
- changed text color to a more bluish tone


Logon version / updater tweak (v82TUPDATE17K):
- version text moved slightly higher
- version text color shifted more blue
- startup popup replaced by a blinking red update icon next to the logon version
- click the version/update area to start the update prompt


v82TUPDATE17N:
- Cleaned up the logon update indicator.
- Replaced the large red circle with a small red refresh glyph directly beside the version number.
- Fixed the weird duplicate/distorted version text by drawing only on the single visible target control.
- Added a global login-window message filter so clicking the version/update area starts the updater even when child controls receive the mouse event.
- Clicking the update glyph now directly starts the update process instead of showing the old startup popup.


v82TUPDATE17O:
- Reworked the logon version display to use a real transparent overlay control on the artwork host.
- Fixed the bugged / distorted version number rendering.
- Moved the version slightly higher.
- Made the update symbol a bit larger.


v82TUPDATE17P:
- Added a login-window mouse message filter so the cursor changes to hand over the version/update hotspot even when a child control receives the mouse.
- The update hotspot click is now caught through the same filter.
- Overlay cursor also switches dynamically based on update availability.


v82TUPDATE17Q:
- Fixed missing update icon after the cursor-hotspot build.
- The update check finishes asynchronously after the login window is shown; the overlay now reattaches/repositions on timer ticks so it expands from version-only to version + update icon as soon as an update becomes available.


v82TUPDATE17S:
- Fixed 17R regression where both version text and update icon disappeared.
- Split the login version text and update icon into two separate transparent controls.
- Only the update icon control repaints/blinks; the version text control stays static.
- Reattach logic is retained so the icon appears after the async GitHub update check finishes.


v82TUPDATE18:
- Added a Reloading EasyCPDLC step to the updater.
- After copying files, the updater now shows an animated reloading bar while the updated EasyCPDLC process starts.
- The updater waits briefly until the restarted process opens a window or until a short timeout expires.


v82TUPDATE18A:
- Fixed updater installer window not appearing reliably after download.
- PowerShell updater is now launched with -Sta so the WinForms installer/reloading dialog can show reliably.
- After launching the updater script, EasyCPDLC now exits the current process immediately so the updater is not stuck waiting for the old PID.
- Updater window is explicitly activated/brought to front before installation starts.


v82TUPDATE18B:
- Replaced the unreliable post-download reloading bar behavior with a simple reliable message:
  "EasyCPDLC is now restarting to install the update."
- The updater still installs and restarts automatically after the user acknowledges the message.
- Simplified the PowerShell restart step again to avoid invisible waiting behavior.


v82TUPDATE19B:
- Replaced the broken manual UI Scale preview with a safer Windows DPI Scaling mode.
- Settings now has: "Use Windows DPI Scaling (larger UI)".
- This does not manually move/resize individual controls.
- When enabled and EasyCPDLC is restarted, Windows scales the whole finished DCDU window bitmap.
- Because mouse input is scaled by Windows too, buttons and hotspots stay aligned.
- This is intended for users on 125%/150%/175% Windows display scaling who need a larger UI.
- A restart is required after changing this option.
