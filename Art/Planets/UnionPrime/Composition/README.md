# Union Prime composition review

2026-11-05. The accepted circular terrain and extracted building artwork are now
assembled as one coherent planet. The user approved this combined composition on
2026-11-05 and requested a basic tower fit study before designing other planets.

Open [planet-preview.png](planet-preview.png) and
[scale-review.png](scale-review.png). The transparent full-resolution composition
is [planet-assembled.png](planet-assembled.png); `thumbnail.png` is a 192-square
nearest-neighbor study derived from that same composition, not separate artwork.

## Placement and supports

- Upper-left/right markets move `(10,16)` / `(-10,16)` source pixels inward.
- Upper-left/right side stands move `(12,5)` / `(-12,5)` pixels inward.
- All other pieces keep their original positions. Every source sprite retains its
  pixels, orientation and scale; no original building was repainted or resampled.
- A separate `guild-support.png` adds 976 opaque masonry pixels in the two gaps
  under the guild's outer threshold. Only previously transparent pixels are filled.
- The accepted terrain draws last, in front of the buildings and supports. All its
  pixels, including the twelve empty sites, are exactly preserved in the assembly.

`composition.json` records source hashes, original and final top-left origins,
integer translations, support placement and the back-to-front assembly order.
All coordinates use the original 1254-square canvas. They are not Unity pivots.

The support repair used built-in imagegen. Its first attempt was translucent and
rejected; the second supplied opaque stone. Only the relevant 354 x 60 source crop
is retained as `guild-support-repair-source.png`; `guild-support-mask.png` records
the selected additions. Exact prompts and crop coordinates are in
`guild-support.prompt.json`. The tool's other output pixels are not used.

## Rebuild

Run from the repository root with Python 3 and Pillow:

```powershell
python Art/Planets/UnionPrime/Separation/separate_structures.py
python Art/Planets/UnionPrime/Circular/build_circular_terrain.py
python Art/Planets/UnionPrime/Composition/build_composition.py
```

Normal rebuilds preserve editable masks and no longer generate the old terrain
previews removed during cleanup. To assemble independently, composite the cropped
guild support at its recorded origin, then the listed structure sprites at their
final origins, then the Circular terrain at `(0,0)`.

## Verification and limits

The build checks unchanged source hashes, complete fill of the defined guild
support gaps, no changes outside those gaps after alignment, and exact foreground
terrain preservation. Full-size and small-size compositions were visually inspected.
Independent layer reassembly and repeatable exports were checked as well.

This is the composition-review milestone, not a finished Unity implementation.
The terrain, structure extraction and combined placement/support treatment are
accepted. Next, check a compact mechanical, rabbit-operated Laser at all twelve
sites before settling crater dimensions or starting another planet. Site widths/depths remain
unequal, and hidden surfaces for arbitrary independent movement are incomplete.
The supports finish the current pose. Scale studies are offline images, not Unity
screenshots. No prefab, scene, runtime code, gameplay behavior or project setting
was changed. No Unity or Play Mode checks were run.
