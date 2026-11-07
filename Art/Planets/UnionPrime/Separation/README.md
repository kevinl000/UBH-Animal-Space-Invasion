# Union Prime visible structure separation

The next composition milestone is ready in [Composition/](../Composition/README.md):
accepted terrain plus unchanged building sprites, four placement adjustments and
separate guild supports. Review that assembled result before Unity integration.


Open the [structure sheet](structures-preview.png). The user accepted these
visible structure assemblies. The current approved terrain is in
[Circular/](../Circular/README.md).

Ten additional assemblies are separated from the repaired working composition:
two upper markets, four side produce stands, two lower shops with their signs
and shrubs, and two bottom kiosks. The original guild facade and foundation are
reused from `../Extraction/`. Each assembly keeps its existing radial orientation.

This is a source-pixel extraction, not a redraw. All twelve empty crater sites
remain in the terrain. The source working image and original B-25 are unchanged.

The user approved the circular terrain with natural grass on 2026-11-04.
`terrain-visible.png` remains only as an input to the Circular rebuild script.
The obsolete terrain and full-planet reassembly previews were removed; normal
rebuilds no longer recreate them. Building sprites and their masks are unchanged.

## Assembly

`separation.json` records each sprite's top-left origin and exclusive crop bounds
on the original 1254 x 1254 canvas. Place `terrain-visible.png` at `(0, 0)`, then:

1. Place `../Extraction/guild-facade.png` and
   `../Extraction/guild-foundation-visible.png` at `(442, 65)`.
2. Place the ten cropped structure PNGs at their recorded `origin_xy` values.

Do not also add `north-terrain-visible.png`: those pixels are already included
in the new terrain layer. No rotation, resampling or scaling is needed.
The selected visible pixels do not overlap, so this inspection assembly matches
the source exactly. Future reconstructed facades should render behind foreground
terrain; this extraction does not define final Unity sorting or mounting pivots.

## Masks and rebuild

Each `*-mask.png` is a full-canvas binary selection. White assigns the original
source pixel to that structure; black leaves it elsewhere. The ten masks are
editable and preserved during normal rebuilds. Use Python 3 and Pillow:

```powershell
python Art/Planets/UnionPrime/Separation/separate_structures.py
```

`--make-masks` deliberately replaces the masks with the traced polygon selections
in the script. Only use it to reset or revise those selections. This workflow
uses the repaired working image directly and does not recreate obsolete exports
with X placeholders.

## Verification and limits

- Source SHA-256 is checked before and after export.
- All selection masks are binary and disjoint from previously assigned pixels.
- Full RGBA reassembly has **zero differing pixels** against the working source.
- The structure and terrain previews were visually inspected; upper-market masks
  were refined to exclude neighboring stand roofs.
- Normal rebuilds preserve masks and reproduce the same saved outputs.

These are **visible fragments**, not yet complete independently movable assets.
Hidden supports and terrain are not reconstructed. Some shared edge pixels,
grass slivers and background foliage still need semantic refinement when those
edges are completed; pixel equality alone does not certify a final sprite boundary.
Signs and shrubs intentionally stay attached to their adjacent building assembly.
The original partial-alpha side repairs are preserved as supplied.

Next: review building/support contact against the approved Circular terrain and
complete hidden edges. Equal site dimensions remain unfinished. No Unity import,
prefab edit or Play Mode check has been performed for this artwork.
