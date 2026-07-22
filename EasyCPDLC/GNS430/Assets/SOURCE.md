# GNS 430 panel artwork source

The faceplate and control sprites in this directory are perspective-corrected derivatives of the straight-on GNS 430 product photograph published by Fieldtech Avionics:

https://www.ftav.com/products/part-011_00280_10.html

They were generated for the user's personal mixed-cockpit simulator interface with:

```text
python scripts/Build-Gns430PanelSprites.py <source-photo> EasyCPDLC/GNS430/Assets --source-url <source-page>
```

`panel-assets.json` records the source page, panel geometry, LCD opening, control bounds, and every generated state. The normal faceplate, pressed keys, range-rocker halves, small knobs, and independently rotated inner/outer encoder rings are embedded into the standalone application.

These derived photographic assets are for personal simulator use. They are not Garmin firmware, are not approved for real-world navigation, and should not be redistributed separately from the project without reviewing the original photographer's terms.
