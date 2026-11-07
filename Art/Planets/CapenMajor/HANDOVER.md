# Capen Major: current artwork and handover

Updated 2026-11-07 on `planets-page`.

The current assembly has sixteen sockets at 22.5-degree intervals, numbered
clockwise from the top. Sockets use the existing floor/rim artwork at 75% of
their previous size; the visual-test Laser follows at 1.5x source scale.
The body size, all source PNGs, and all five facade transforms are preserved.
Facade layout is deferred to the next pass. This supersedes the twelve-site
placement and reference-size normalization in the production history below.

The subsequent shoulder repair removes the stretched old-station border on
both sides of the upper cap. Preserve the continuous source-contact guides and
right shoulder extension to x=940 in `build_review.py`; reverting to the first
opaque source row brings back the black fragments. Socket positions are unchanged.

The assembly is exported into eight Unity sprite layers, with free
Laser placement at all sixteen sites. Open the
existing Capen Major scene and press Play. See [Unity testing](UNITY_TESTING.md)
for controls, verification and rebuild instructions. The production notes below
record the original artwork decisions; `unity-import.json` records the runtime export.

Use [the current occupied composition](planet-occupied.png) and
[empty/occupied review](page-review.png) for continued work. The latest refinement
keeps the improved shallow crater shape, clean steel contours and four restored
pixel-art facades. The enlarged administration building is now the top-center
landmark at the user-requested 0.30 scale. The flags now sit at 10 o'clock,
mirroring the station at 2 o'clock; lower facades retain their original contacts. The admin's foundation
and steel ground follow a circular cap with its center at y=183. The rigid
administration facade now has a deeper curved stone foundation beneath its wings.
Its stonework reaches the ground at both corners; the sprite's dark bottom border
is buried behind the hull rather than stretched into visible wedges.
One-pixel strips replace the earlier faint four-pixel bands. The station has subsequently
been positioned at 2 o'clock and its lower facade curved to seat both bottom corners
against the hull; see [station contact closeups](station-review.png).
Runtime preparation is complete; see `UNITY_TESTING.md`.
See [administration empty/occupied closeups](admin-review.png) and the earlier
[facade before/after](facade-review.png).

![Current Capen Major](planet-occupied.png)

## File organization

The [source workspace README](README.md) maps the five current source sprites,
exact prompts, assembly script, measured placements, SVG assemblies and PNG
reviews. These now live directly in `Art/Planets/CapenMajor/`, like Union Prime.
The `TerrainCut` wrapper and unused rough-edge/floor v1 sources and prompts were
removed in the earlier workspace cleanup.
The subsequent facade repair adds `facades-pixel-source-v1.png`: only the four
regions in `build_review.py` are used, preserving the original globe. The whole
generated redraw is not a replacement body. Recess layers use the original
alpha with facade regions excluded, so rim ends cannot protrude onto buildings;
terrain cuts also leave facade artwork intact. The current assembly clips and
places each facade independently behind the hull. The upper hull cap and admin's
stone foundation follow a shared circular contour using overlapping SVG strip transforms.
Windows, doors and upper floors remain rigid. See `admin_ground_curve` in the manifest and the source README.
See [junction checks](junction-review.png).
Earlier body, facade, bay and layer-export experiments are obsolete and must not
be used as current inputs or fit evidence.

`admin-facade-source-v1.png` is the separate administration sprite. Its source
origin, scale, radial angle and anchor are recorded in `assembly.json` under
`additional_facades`. It renders behind the hull and recess floors, excluded
from the supporting hull mask. See `prompt-admin-facade-v1.txt` for provenance.
Its scale is 0.30, angle 0, anchor `(608, 192)`.
The other four transforms are recorded under `facade_placements`.

## Future planet design: use these two directories

Use only the current planet-design references in:

- `C:\UBHackathon2026\Art\Planets\UnionPrime` ([source guide](../UnionPrime/README.md)).
- `C:\UBHackathon2026\Art\Planets\CapenMajor` ([source guide](README.md)).

Use repository-relative equivalents on another device. Begin with each folder's
`page-review.png` and `all-sites-aiming.png`; Capen's
[matched-width comparison](reference-comparison.png) shows them together.

Focus on **crater shaping and shadow/recess shading**: broad shallow cuts,
short banks, a readable floor, restrained darker recessed surfaces/contact
shadows, and a low foreground edge overlapping the bottom of the tower foundation.
These cues should make the tower feel seated in the planet, even when the crater
is empty. Check radial orientation, usable foot clearance and facade support.
**Rough edges are not required.** Match the contour and shading to the planet's
material: Union Prime's dirt can be irregular; Capen's steel stays smooth and
continuous. Avoid deep black cavities, broad shadow bands, trays and tubular rims.
Adapt material and identity without inventing site counts, scales or mechanics.

## Decisions to preserve

- Spherical segmented metal body, quiet center and no orange body markings.
- Sixteen shallow radial recesses, spaced 22.5 degrees apart; smooth steel edges and restrained shading.
- Enlarged administration at twelve o'clock: flat stepped roof, office windows,
  peach/slate palette and a central `ADMIN` sign.
- Ground contact must curve with the planet. Preserve the curved hull cap and
  matching lower-facade deformation when exporting; a raw rectangular sprite
  would reintroduce the rejected flat ledge. Admin source scale remains 0.30.
- Bus station at 2 o'clock (60 degrees) and 0.70 source scale, anchor `(1044, 443)`, with
  its lower facade fitted to the hull using `station_ground_curve`;
  cargo depot and shelter retain their original positions and full source scale.
  Flag plaza at 10 o'clock (300 degrees), anchor `(182.4, 449)`, full source scale;
  its base is tucked 12 pixels inward to close the surface gap.
  The lower contact band also follows the actual hull via `flag_ground_curve`;
  the shift alone left a triangular gap at the upper end. Keep the contour fit.
  Facades project outward behind the hull and recesses.
  Keep gaps for future additions; do not force equal-angle spacing.
- Taller US center flag and two shorter plain blue flags at the plaza.
- The exposed old plaza edge has a separate hull-clearance mask. Its join at
  `(340, 268)` blends into the cap over 50 pixels; the cap outline clips strip
  protrusions. Keep these fixes when exporting to avoid green remnants and steps.
- Both lower facade connector webs remain removed. No lateral filler bridges
  or handles.

## Verification and next implementation work

The sixteen-socket rebuild passes 992 foundation contact samples (62 per site).
The reviews show -60, 0 and +60 degree aiming poses at all sixteen sites.
Sockets are 126 source pixels wide; the closest adjacent mounts leave over
38 source pixels beyond that width. Mount angles are exactly 22.5 degrees apart,
with radii measured against the existing hull alpha around `(608, 695)`.

Original source hashes, facade placements/curves, body diameter, and display
canvas are preserved. Full-planet previews retain the 1392-by-1420 canvas and
viewBox `-80 -100 1392 1420`. See `UNITY_TESTING.md` for runtime verification.
Full aiming/projectile-clearance sweeps and future-tower fitting remain untested.
Facade layout and medium-planet resizing are separate follow-up tasks.

The Unity export uses separate sprites for each facade, rear floor, terrain and
steel fronts, preserving these masks and curves. The visual prefab now provides
free Laser placement; the earlier artwork cleanup did not change gameplay.
