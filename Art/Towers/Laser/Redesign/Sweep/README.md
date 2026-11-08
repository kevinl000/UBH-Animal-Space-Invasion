# Laser sweep assembly

**Current revision, 2026-11-08:** use [the main Laser handoff](../../README.md),
[assembly.json](assembly.json) and [preview.html](preview.html). Current sources
are **base-generated-v5.png** (focused expression and lower fixed cyan screen) and
**gun-generated-v3.png** (single laser cannon). Exports keep their 70x72/24x33
canvases and original ground/hinge coordinates; the muzzle is now **(14.5, 0)**,
or local **(0.025, 0.28)** from the hinge at 100 PPU. Existing Unity sprite
paths/GUIDs are preserved, and `LaserTowerVisual.prefab` supplies a visual rig.

The v5 expression edit matches the Sniper operator's focused eyes/brows, small
square nose and tiny coral mouth. Its prompt is
[base-expression.prompt-v5.txt](base-expression.prompt-v5.txt). The v3 source
with the lowered screen remains preserved. Current portrait: **portrait-v3.png**.

The rebuild script now reads the current manifest's uniform export transforms.
Its refreshed checks cover 241 gun/base poses, a conservative 7-pixel projectile
corridor and the historical twelve-site terrain study. These remain sampled
offline checks; no Unity validation was run. See the main handoff for current
import/attachment details and the limits of the older terrain placement study.

## Historical 2026-11-05 notes (superseded source and muzzle details)

2026-11-05: the user accepted this compact sweep design ("lets go with this for
now") and requested removal of unused versions. This is the retained assembly.
The original single-piece concept, tall-mast experiment and static intermediate
have been removed. Current source images, exported sprites, previews, prompts,
mount coordinates and rebuild script are retained here.

**Update, 2026-11-05:** the user selected **150%** from the completed
scale and seating studies (removed during the requested cleanup). This directory remains
the unchanged 100% source assembly/reference. The user subsequently authorized
the top-crater prototype, followed by the now-accepted `Art/Planets/UnionPrime/`.
The old planet prototype directories were removed on 2026-11-06. Current seating
comes from `TerrainPolish/polish.json`; the planet paths in `build_sweep.py` now
read the retained final terrain. Its 100%/outward-offset study is historical,
not the current 150% planet fit. `rotation-study.json` retains historical input
hashes for provenance; it is not a dependency list for the current planet rebuild.

**Previous handover fit issue:** the user says the tower looks too small and does not
sit convincingly in the crater. The design/mechanism is accepted; its current
scale and seating are not final. Compare whole-tower sizes of 100%, 125% and
150% on unchanged terrain first, then mounting depth and a foreground soil lip.
These are untested review sizes. Revisit the current outward offset and recheck
clearance after changes. Propose crater revisions only after that comparison;
see Next-session priority in the root `PLANETS_PAGE_PLAN.md` for the handover.

- [Animated sweep](sweep.gif).
- [Five-angle comparison](sweep-review.png).
- [Twelve sites, both limits and neutral](twelve-site-sweep.png).
- [Page-scale comparison](page-scale-sweep.png).
- Separate transparent [base/operator](base-sprite.png) and [gun](gun-sprite.png).
- [Pivots, source hashes, placements and checks](rotation-study.json).

## Assembly

The rabbit, seat, low armored chassis and short angled bracket are stationary.
Only the complete chunky gun turns. Its rounded rear casing covers the bracket's
socket. The bracket moves the joint to the right of the rabbit rather than above
its ears on a tall mast. The muzzle belongs to the rotating gun.

The base is 70 x 72 pixels, the gun 24 x 33, and the neutral assembled bounds
76 x 76. The older tall design was 70 x 119. Use the same pixel scale for both
parts; the base draws behind the gun. No pose-specific sprite swaps are needed.

Coordinates are pixel-edge coordinates from the top left of each exported PNG:

| Part | Point | X | Y |
|---|---|---:|---:|
| Base | Ground mount | 34 | 71 |
| Base | Gun socket | 64 | 24 |
| Gun | Rotation pivot | 12 | 28 |
| Gun | Muzzle reference | 12 | 3 |

Align socket and pivot. Neutral fires along local up/outward. The allowed visual
arc is -60 to +60 degrees relative to outward, with the rabbit fixed. Preview
angles are clockwise; Unity Z angles use the opposite sign. A future Unity setup
can pivot the base at normalized `(34/70, 1-71/72)` and the gun at
`(12/24, 1-28/33)`, placing the gun pivot `(30, 47)` pixels right/up from the base
mount and its muzzle `(0, 25)` pixels from that pivot. Divide these offsets by
the shared pixels-per-unit. Both parts inherit the planet's orientation; only
the gun receives the local aiming rotation.

The animation demonstrates reachable poses. It does not prescribe continuous
sweeping when idle or implement target acquisition, firing cadence or tracking.

## Crater placement and verification

The old fit-study mounts placed this shorter gun partly inside foreground terrain
at some side/lower sites. Every tower is therefore mounted **16 source pixels
farther outward**, using the same rule and scale at all twelve sites. No terrain,
facade or crater artwork changed. Original mounts are preserved in
[site-mounts.json](site-mounts.json); original and adjusted coordinates are in the
output manifest. A foreground soil lip and final embedding appearance are still pending.

The rebuild checks 241 angles at 0.5-degree intervals across the complete arc:

- Zero opaque gun/base overlap outside the intended joint region (a 12-pixel
  radius covers the rear casing, socket and upper bracket). The rabbit, controls
  and chassis are outside that exclusion.
- The gun remains connected to the support at every sample.
- A one-pixel firing ray from the muzzle clears the stationary base/operator.
- At all twelve adjusted mounts, the gun and firing ray clear foreground terrain:
  2,892 site/angle combinations. Rear facades are scenery, not collision masks.
- All input hashes stay unchanged. Repeated rebuild outputs match.

These are sampled offline raster checks at alpha >=128, not continuous geometry
proofs or Unity Play Mode results. The thin ray does not validate an eventual
projectile's width. The screenshots retain facade overlap at crowded sites, and
generated shading/partial alpha still need the final production pixel-art pass.

## Source and rebuild

Built-in imagegen created [base-generated.png](base-generated.png) and
[gun-generated.png](gun-generated.png), using the preceding static concept
(removed during the user-requested cleanup; reference SHA256
`7b4298bcb36c9fc6edb3c8ec26a407898e293dd9854a06c4278593c8276461c3`).
Exact prompts: [base](base.prompt.md),
[gun](gun.prompt.md), [gun edge/style correction](gun-refinement.prompt.md).
The rabbit's appearance and pose were preserved visually; this is not a claim
of pixel-identical preservation through image generation. Rebuilding needs only
the two retained generated images, local mount coordinates and approved planet
inputs; it does not need any deleted concept. Cleanup preserved all current PNG
and GIF hashes and all Union Prime image hashes.

Run `python Art/Towers/Laser/Redesign/Sweep/build_sweep.py` from the repository
root with Pillow installed. This workstation can use
`$env:PYTHONPATH = 'C:/UBHackathon2026/.git/menu-work/python'` in PowerShell first.
The script crops alpha >=128 source bounds, preserves alpha inside, uniformly
downsamples with nearest-neighbor filtering and applies rigid transforms for the
study. It does not repaint source art.

No Unity assets, scenes, runtime scripts, managers or gameplay data changed.
No Unity checks were run. When importing later, use the documented pivots and
common pixel scale, then verify both limits, intermediate poses, muzzle alignment,
joint continuity and soil occlusion at all twelve sites in Unity 6000.6.7f1 via
the user's Hub workflow. This settles the offline aiming assembly; production
import and crater finishing are separate remaining steps.
