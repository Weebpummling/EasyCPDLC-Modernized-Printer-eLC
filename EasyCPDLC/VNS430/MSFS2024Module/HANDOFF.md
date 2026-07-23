# VNS430 MSFS module handoff

## Current state

The desktop VNS430 and its shared EasyCPDLC backend are in place. The optional
MSFS 2024 module currently provides the starting point for the in-cockpit
installation, bridge protocol, VNS430 L-vars, and MobiFlight profiles. Final
in-simulator positioning and SDK-dependent 3D work must be completed on a
machine with the MSFS 2024 SDK installed.

## Next phase, in priority order

1. **Position the 3D panel correctly in the cockpit.**
   Load each supported PMDG 737-800 preset, calibrate the panel's translation
   and rotation against the intended printer-panel opening, and confirm that it
   remains correctly aligned from normal pilot viewpoints.

2. **Replace the borrowed stock model with a dedicated VNS430 panel.**
   Build VNS430-specific 3D geometry and materials, connect its display,
   buttons, dual rotary encoders, annunciators, and animations to the existing
   `EASYCPDLC_VNS_*` L-vars and private bridge protocol, and verify that mouse,
   MobiFlight, and rendered panel states remain synchronized.

3. **Make the LCD lettering heavier and emulate an older display.**
   Increase the thickness of the LCD text and line strokes, then add restrained
   post-processing softness/fuzziness to reproduce the slightly bloomed,
   lower-resolution appearance of an older LCD without reducing readability.

## Completion checks

- The panel is correctly positioned in every supported aircraft preset.
- No stock Garmin navigation functions or identifiers are used for VNS430
  interaction.
- Buttons and both rotary encoder rings animate in the direction and state
  reported by the VNS430 L-vars.
- The in-cockpit LCD matches the desktop VNS430 page and annunciator state.
- LCD softness is visible at normal viewing distance but text remains legible.
- Restart and package-cache behavior is retested and documented.

