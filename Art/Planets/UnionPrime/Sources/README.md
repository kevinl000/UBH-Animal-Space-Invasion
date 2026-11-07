# Live rebuild inputs

These are the minimum inputs retained from the deleted prototype pipelines.

- `facade-layout.json`: source canvas and thirteen facade origins.
- `FacadeMasks/*.png`: lossless grayscale alpha channels extracted from the
  previous source sprites. The old colored pixels are no longer needed.
- `site-layout.json`: initial twelve-site positions consumed by
  `build_polish.py` before it measures the current painted soil and fits contact.

Use `../polish.json` for the resulting runtime mounts. These seed positions are
not a replacement for the final measured mounts. The migrated inputs reproduce
the existing final artwork exactly.
