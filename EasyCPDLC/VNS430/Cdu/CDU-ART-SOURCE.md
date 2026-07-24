# CDU panel artwork — source and provenance

`../Assets/Cdu/cdu-panel.png` is the LSK-only CDU display mode's panel artwork: a
Boeing 737NG-style FMC CDU (screen, 6+6 line-select keys, the function-key block,
alpha/numeric keypad and CALL/FAIL/MSG/OFST annunciators). It is the only artwork
embedded in the build.

- **Source:** supplied by the repository owner as their own artwork
  (`CDU Image own.png`), for use on this personal/own-use branch.
- **Layout:** the per-key rectangles and the LCD screen rectangle in
  [`CduPanelLayout.cs`](CduPanelLayout.cs) were measured from this artwork by
  detecting each key's printed legend, then extracting the key bounds around it.
- **Rendering:** `CduDisplayPanel` draws the artwork as the panel background and
  renders the 24x14 character screen into the screen rectangle. A pressed key is
  shown by darkening that key's region of the same artwork at render time, so no
  separate per-key sprite files are shipped.

**Redistribution:** confirm the artwork's licensing before publishing this branch
or the image anywhere public — mirroring the personal-use provenance recorded for
the VNS430 Garmin artwork in `Assets/SOURCE.md`.

Flight simulation only; not approved for real-world use.
