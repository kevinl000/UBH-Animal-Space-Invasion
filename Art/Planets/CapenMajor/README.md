# Capen Major: art sources and previews

This is the art-production workspace for the current Capen Major artwork.
Start here when editing or using it as a reference for another planet.
The smooth steel crater edges retain the approved broad, shallow recess shape.

| Location | Purpose |
| --- | --- |
| This directory | Five current source sprites, exact generation prompts, assembly script, measured placements and review compositions. |
| [HANDOVER.md](HANDOVER.md) | Design decisions, future-planet references and remaining implementation work. |
| `Assets/` | The current artwork has not been exported to Unity; existing Capen scene/prefab placeholders are not this assembly. |

## Current reviews

- [Bus station curved base and corner alignment: empty and occupied](station-review.png).
- [New campus administration facade: empty and occupied](admin-review.png).
- [Facade pixel-art repair: before and after](facade-review.png).
- [Current facade/recess junctions](junction-review.png).
- [Empty and occupied composition](page-review.png).
- [All twelve radial sites at -60, 0 and +60 degrees](all-sites-aiming.png).
- [Locally upright foundation closeups](contact-review.png).
- [Top-site contact detail](top-review.png).
- [Union Prime comparison at matched globe width](reference-comparison.png).
- Transparent full compositions: [empty](planet-empty.png), [occupied](planet-occupied.png).

The PNG compositions are previews, not runtime layers. Their self-contained SVG
assemblies retain bitmap artwork, masks and placement transforms. No separate
prototype pipeline remains.

## Current sources and exact prompts

All five source sprites were produced with built-in imagegen. Their generated
pixels are retained unchanged.

| Source sprite | Exact prompt |
| --- | --- |
| [Body and four facades](body-clean-source-v1.png) | [Body prompt](prompt-clean-body-v1.txt) |
| [Pixel-art facade redraw](facades-pixel-source-v1.png) | [Facade prompt](prompt-facades-pixel-v1.txt) |
| [Separate campus administration facade](admin-facade-source-v1.png) | [Administration prompt](prompt-admin-facade-v1.txt) |
| [Smooth foreground steel edge](steel-edge-source-v2.png) | [Edge prompt](prompt-edge-v2.txt) |
| [Smooth rear floor](steel-floor-source-v2.png) | [Floor prompt](prompt-floor-v2.txt) |

Version suffixes identify the retained generations, not parallel pipelines.
The prompts record the original edits; superseded input sprites are historical
provenance and are not needed to rebuild. Continue edits from these current sources.

`build_review.py` measures the front edge's alpha contour and cuts matching
openings in the body. Back to front: five independently placed facades, rear floor,
cut body, separate Laser base/operator and gun, then the low steel front edge. The hull alpha clips
the floor and edge to the surface, excluding facades. One profile serves all twelve
radial orientations. [assembly.json](assembly.json) records mounts, contact
samples and source hashes.

The only external artwork dependencies are the shared Laser base/gun in
`Art/Towers/Laser/Redesign/Sweep/` and the occupied Union Prime reference in
`Art/Planets/UnionPrime/`. The 2.0x Laser source scale normalizes Capen's roughly
986px globe against Union Prime's roughly 730px globe at 1.5x. It is not a gameplay
size or balance decision.

## Future planet design references

Use [Union Prime](../UnionPrime/README.md) and this directory as the two current
planet-design references. Focus on shallow crater shaping and shadow/recess
shading: a readable floor, short banks, restrained darker recess/contact shading
and a low front edge that seats the tower foundation. Review both empty and
occupied sites at every radial orientation and at comparable display sizes.
Rough dirt edges are optional and material-dependent; Capen's manufactured metal
uses clean continuous contours. Preserve depth without heavy black cavities,
broad shadow bands, raised trays or tubular rims.

## Rebuild

From the repository root, with Python and Pillow:

```powershell
python Art/Planets/CapenMajor/build_review.py
```

Python must have Pillow installed.
The script writes eleven SVGs and `assembly.json`; it does not repaint PNG sources
or regenerate PNG reviews. Render each SVG through an HTML `img` in headless Chrome at its declared
width and height, device scale 1, with scrollbars hidden and a transparent default
background to update its matching PNG. Existing PNG reviews need no rebuild to view.

## Facade pixel-art repair, 2026-11-06

Restored the bus station, flag plaza, transit shelter and cargo depot to stepped
pixel outlines and discrete shading using built-in imagegen. The original body
was the edit target; Union Prime's occupied composition supplied the pixel-art
style reference. The exact prompt is retained above.

The generated image also restyled the globe, so the assembly uses only its four
facade regions. `FACADE_REGIONS` in `build_review.py` defines the replacement
boundaries, also recorded in `assembly.json`. The original body supplies all
remaining artwork and, with facade regions excluded, the hull mask for tower
recesses. The original source PNGs, steel edge/floor, tower placements and tower sprites remain
unchanged. Continue facade edits from the redraw, and body edits from the original
body; do not substitute the whole redraw for the original body.

### Facade/recess junction correction

The original silhouette mask included buildings, allowing steel rim ends to
extend onto the station and transit shelter. Recess floors and front edges now
use the hull alpha with facade regions excluded. Terrain openings cut the hull
only; facade artwork stays intact behind towers. No source artwork or tower
positions changed. All nine previews were refreshed, including closeups of the
four reported junctions. All 744 foundation samples pass against the stricter
hull mask, and the rebuild now fails if a foundation loses support.

## Verification and remaining work

Original source hashes are preserved. The top tower placement now follows the
spherical cap as described below; the other eleven placements are unchanged.
The four repaired junctions were visually checked in empty and occupied views;
all 744 foundation contact samples pass against the hull mask.

Runtime layer/mask export remains pending; facade separation currently uses SVG
clips. No full aiming/projectile-clearance sweep, future-tower fitting, final HUD-scale review,
Unity Play Mode check or standalone build was run. No Unity assets, metadata,
scenes, prefabs, managers or gameplay were changed.

The former `TerrainCut` wrapper, obsolete iteration folders and rough steel
edge/floor v1 sources were removed in the earlier cleanup. Git history retains
those experiments; the five sources listed above are the current rebuild inputs.

## Campus administration facade, 2026-11-06

The separate transparent administration-building sprite is the top-center
landmark. This is a stylized campus office inspired by
Capen Hall's administrative role, not an architectural replica. Flat stepped
rooflines, three floors of office windows, a recessed entrance and an `ADMIN`
sign distinguish it from the existing station. Peach concrete and slate-blue
trim follow the existing facades. Built-in imagegen produced the retained source;
the exact prompt and reference are recorded above.

The SVG assembly places it at `(608, 192)`, upright, at 0.30 source scale around
source foundation center `(793, 946)`. It sits behind the hull and recess floors
and does not contribute to the
supporting hull mask. Placement is recorded in `assembly.json` under
`additional_facades`. Source pixels remain unchanged; the SVG renderer performs
the placement.

### Current facade layout

Keep the admin's 0.30 scale and original lower facade contacts. The flag plaza
at `(182.4, 449)` mirrors the station's upper-right angle on the left, tucked
12 pixels inward along its radial axis to seat its base. Gaps remain for future facades;
do not force equal angular spacing. Foundation checks alone do not validate
facade appearance.

| Facade | Position / angle clockwise from top | Source scale |
| --- | --- | --- |
| Administration | Top center / 0 degrees | 0.30 |
| Bus station | Upper-right / 2 o'clock / 60 degrees | 0.70 |
| Cargo depot | Original lower-right / 135 degrees | 1.00 |
| Transit shelter | Original lower-left / 225 degrees | 1.00 |
| Flag plaza | Upper-left / 10 o'clock / 300 degrees | 1.00 |

`FACADE_PLACEMENTS` removes each source facade's original orientation before
applying its new placement. `assembly.json` records those four transforms under
`facade_placements`. All five facades now render behind the hull and recesses.
The original source extraction polygons are unchanged. The original flat station
footprint is refitted into a curved upper cap; the admin's lower facade and
foundation follow the same contour. Its anchor exposes the entrance and
foundation at the retained scale.
The old plaza's green/dark fringe is removed with a separate hull mask, leaving
the flag sprite extraction intact. The exposed cap joins the steel at `(340, 268)`
and blends back to the existing circle over 50 pixels. A cap outline clips
overlapping strips so they cannot protrude above the intended silhouette.
The flag plaza's contact band below local y=-35 is fitted to the actual hull in
upright local coordinates; the flags and upper poles remain rigid. Colored base
pixels overlap the hull by at least four pixels, burying the dark extraction
border instead of leaving a triangular gap. `flag_ground_curve` records the fit.
The generated PNG sources are unchanged. Both transparent full-planet previews
use a 1392-by-1420 canvas with viewBox `-80 -100 1392 1420`; this adds 100 pixels of
top margin and 80 pixels on the left to retain the flags without changing assembly coordinates. Use the manifest coordinates,
not preview PNG coordinates, for future runtime placement.

All ten SVG/PNG reviews were rebuilt. The administration closeup, empty/occupied
composition and three aiming poses were visually inspected. All 744 foundation
samples pass; previous source hashes and facade extraction boundaries are preserved.
The top tower mount follows the spherical cap; the other eleven mounts are unchanged.
This is art-workspace work only; Unity
export, Play Mode and full projectile-clearance verification remain pending.

### Curved administration ground contact

The old shallow cap still looked flat. The upper hull now follows a circular
arc, `695 - sqrt(512**2 - (x-608)**2)`, joining the existing shoulders and raising
the center to y=183. One-pixel SVG strips with one-pixel horizontal overlap replace
the visible four-pixel bands; two pixels of overlap at y=340 close the join to
the original lower hull.
The composed hull also normalizes near-opaque alpha (gain 1.07) after the strips
are combined. This removes the faint bands caused by antialiased strip boundaries
in reduced previews, without blurring the artwork or changing source PNGs.

The administration is raised to anchor `(608, 192)` at the same 0.30 source scale.
Only its bottom stone foundation below y=180 extends to the curve, with five
pixels of hull overlap. Windows, doors and upper floors remain rigid. The
foundation becomes deeper at the wings to support the building on the dome.
The contact uses the stonework's bottom at source y=930, above the sprite's dark
bottom outline. This extends the masonry to both ground corners and hides the
stretched dark border beneath the hull instead of leaving apparent gaps.
Source artwork and all other facade transforms are unchanged.

The 12 o'clock recess and preview tower move upward by 54 pixels with the new
surface: the horizon is now `(608, 183)` and the tower foundation mount is
`(608, 224.15)`. Their relative seating, size and orientation are preserved.

`admin_ground_curve` in `assembly.json` records the contour and transform limits.
Future runtime layer exports must preserve this deformation, rather than importing the unmodified rectangular
admin sprite alone. Review [the curved contact closeup](admin-review.png).

### Bus station curved ground contact

The station sits at approximately 2 o'clock (60 degrees), at anchor `(1044, 443)`
and the retained 0.70 scale, leaving more room beside the administration building. Its lower facade
now follows the measured supporting hull, including both bottom corners. Below
local y=-70, overlapping one-pixel SVG strips extend the foundation four pixels
into the hull; the roof and bus sign stay rigid. The contact is resampled along
the right-hand hull at this placement. `station_ground_curve` in the
manifest records the fit and sampled contact coordinates.

Use [station-review.png](station-review.png) to inspect empty and occupied contact.
The station adjustment preserves source PNGs and other facade placements;
the subsequent spherical-cap adjustment moves only the top tower mount.
all 744 tower foundation checks pass. Preserve this deformation during future
Unity export. This remains an art assembly, not a runtime Unity change.
