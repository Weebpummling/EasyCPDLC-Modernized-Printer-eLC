# VNS430 panel artwork source

The VNS430 faceplate and control sprites in this directory are perspective-corrected derivatives of a straight-on GNS 430 product photograph published by Fieldtech Avionics:

https://www.ftav.com/products/part-011_00280_10.html

They were generated for the user's personal mixed-cockpit simulator interface with:

```text
python scripts/Build-Vns430PanelSprites.py <source-photo> EasyCPDLC/VNS430/Assets --source-url <source-page>
```

`panel-assets.json` records the source page, panel geometry, LCD opening, control bounds, calibrated encoder pivots, and generated states. The rectangular key crops remain fixed while runtime gradients create the press depth, preventing the photographed bezel from jumping. Each dual encoder is split into an inner and outer transparent layer; only the selected ring is rotated around its calibrated mechanical axis while the recess and surrounding panel remain stationary.

These derived photographic assets are for personal simulator use. They are not Garmin firmware, are not approved for real-world navigation, and should not be redistributed separately from the project without reviewing the original photographer's terms.
