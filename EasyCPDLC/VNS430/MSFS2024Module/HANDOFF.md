# VNS430 MSFS module handoff

## Current state

The desktop VNS430 and its shared EasyCPDLC backend are in place. The optional
MSFS 2024 module currently provides the starting point for the in-cockpit
installation, bridge protocol, VNS430 L-vars, and MobiFlight profiles. Final
in-simulator positioning and SDK-dependent 3D work must be completed on a
machine with the MSFS 2024 SDK installed.

All VNS430 work lives on the `gns430` branch. `master` is the focused
Print/eLC fork and deliberately contains none of it, so make VNS430 changes on
`gns430` and never on `master`.

## Next phase, in priority order

1. **Position the 3D panel correctly in the cockpit.**
   Load each supported PMDG 737-800 preset, calibrate the panel's translation
   and rotation against the intended printer-panel opening, and confirm that it
   remains correctly aligned from normal pilot viewpoints.

   The first attempt at this shipped a panel that appeared nowhere. The cause
   was that the attachment carried only `attach_offset`, set to the absolute
   glTF world position of `Selcal_Dzu_Remove`. MSFS positions a
   `SIM_ATTACHMENT` relative to a named node, and no interior attachment in any
   surveyed package positions itself by raw offset. The package now anchors to
   `bl_Ped` with a small node-relative offset; see the module README. The
   remaining work is calibrating that offset in the simulator, not finding the
   mechanism.

   The offsets are derived from glTF coordinates, so the simulator's axis order
   and signs still need confirming against what is actually rendered.

2. **Replace the borrowed stock model with a dedicated VNS430 panel.**
   Build VNS430-specific 3D geometry and materials, connect its display,
   buttons, dual rotary encoders, annunciators, and animations to the existing
   `EASYCPDLC_VNS_*` L-vars and private bridge protocol, and verify that mouse,
   MobiFlight, and rendered panel states remain synchronized.

`Vns430Form` still contains the original hand-drawn panel and page routines
(`DrawStatusPage`, `DrawKnob`, `DrawPanelButtons`, `DrawFastener` and their
helpers). Nothing calls them any more; they are kept only because they record
the intended panel geometry and page layout, which is useful reference for
task 2. Delete them once the real 3D panel exists.

## Done, pending in-simulator confirmation

**Make the LCD lettering heavier and emulate an older display.** Implemented in
`Vns430LcdRenderer.ApplyLcdAppearance`: a 4-neighbour brightness dilation
thickens glyph and rule strokes, then a 3x3 box blur adds the softness of an
aged passive-matrix panel. The desktop bitmap is streamed to the in-cockpit
gauge unchanged, so this affects both surfaces.

Two constants at the bottom of that file are the calibration knobs:

```text
LcdBloom    = 0.45   strength of the stroke-thickening bleed (0..1)
LcdSoftness = 0.25   strength of the final softening blur (0..1)
```

Tune those against reference photos on the simulator machine. `Render` also
takes `applyAppearance: false` to get the raw pixel-exact raster for
comparison. The pass allocates nothing per render and adds about 0.7 ms, so
raising the weights is cheap; only the two constants should need to change.

## Completion checks

- The panel is correctly positioned in every supported aircraft preset.
- No stock Garmin navigation functions or identifiers are used for VNS430
  interaction.
- Buttons and both rotary encoder rings animate in the direction and state
  reported by the VNS430 L-vars.
- The in-cockpit LCD matches the desktop VNS430 page and annunciator state.
- LCD softness is visible at normal viewing distance but text remains legible.
  Still to be judged on the simulator; only the desktop panel has been seen.
- Restart and package-cache behavior is retested and documented.

## Note for whoever picks this up

The desktop panel caches its rendered LCD and only re-renders when
`Vns430LcdState.Fingerprint()` changes. If you add anything to the display,
add it to that fingerprint too, or the panel will show a stale frame. The test
`Vns430Tests.LcdState_FingerprintCoversEveryRenderedProperty` fails if a new
state property is added without being covered, so follow what it tells you.
