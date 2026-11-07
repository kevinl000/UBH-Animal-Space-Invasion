# Shared crater: twelve-site fit review

2026-11-05. After approving the top crater, the user requested cleanup of unused
files and continuation with other-site checks. This study rigidly places that
same profile at all twelve existing site angles. It does not replace the accepted
planet source or import anything into Unity.

- [Empty and occupied whole-planet views](page-review.png).
- [Crowded upper, side, upside-down and upper-side closeups](orientation-closeups.png).
- [Every site at both aiming limits and neutral](all-sites-aiming.png).
- [Placements, source hashes and measured exceptions](fit-study.json).

## Placement

All mounts use radius **330** about `(627,610)`, derived from the accepted top
mount `(627,280)`. The previous study's irregular mount radii are retained in
metadata for comparison, not reused as per-site depth adjustments. Existing
site angles remain; site 12 uses exact local up to preserve its accepted image.
Crater size stays 100% of the approved template and every Laser stays 150%.

The same terrain patch, edit mask and foreground floor edge rotate together.
Only the mask-selected terrain changes in the exported fit copy. Original
facades reassemble unchanged behind it. The foreground is clipped to the
installed patch, so its extraction rectangle cannot overwrite nearby ground.
The accepted top crater remains pixel-identical inside its edit mask.

Upper-site blending masks share 76 pixels: 56 between sites 12/1 and 20 between
12/11. These contain grass/blue/transparent margins, not soil cores. The top
reference owns those pixels, followed by clockwise placement; an overlap of
soil cores fails the build. No crater or tower is resized to hide crowding.

## Results and remaining visual work

All **2,892** site/angle combinations pass the sampled gun/terrain and one-pixel
muzzle-ray/terrain checks, at 0.5-degree intervals across +/-60 degrees. The
foreground never covers the gun. It covers 584-589 opaque base pixels per site,
consistent with the accepted top site's 587 pixels.
Two consecutive builds produced identical exported image and metadata hashes;
all accepted prototype, planet and tower reference images retained their hashes.

The shared shape seats the tower at the side and upside-down sites, but the
surrounding terrain is not finished. The old sites were unequal; copying the
top template leaves portions of their old inner dirt/grass perimeter. There
are also blue texture joins where the top patch meets different terrain.

| Site | Original soil pixels remaining outside the template |
|---|---:|
| 3 | 197 |
| 4 | 511 |
| 5 | 544 |
| 6 | 345 |
| 7 | 638 |
| 8 | 417 |
| 9 | 23 |

The other five sites have zero detected leftover soil within the measured
neighborhood. This color-based count excludes green remnants and does not
establish complete visual blending. The side and bottom closeups deliberately
show the remaining second contour; it has not been hidden by per-site scaling.

Empty craters also expose incomplete rear surfaces under the guild and some
nearby facades. No building art/support was changed. Filled upper/side/lower
sites retain overlap with decorative shops, which are not collision blockers.

Next: remove the superseded inner soil/grass edges at sites 3-9 and blend each
patch into its surrounding blue/green terrain, keeping the shared crater profile
and tower transforms fixed. Resolve rear support/back-wall presentation within
its agreed scope before production. Review the resulting empty and occupied
compositions before Unity integration. This study validates sampled clearance,
not final visual acceptance, projectile width, future tower capacity or gameplay.

## Rebuild

From the project root, with Pillow available:

```powershell
python Art/Planets/UnionPrime/CraterPrototype/build_prototype.py
python Art/Planets/UnionPrime/CraterPrototype/FitReview/build_fit_review.py
```

On this workstation set `$env:PYTHONPATH = 'C:/UBHackathon2026/.git/menu-work/python'`
first. The shared transforms are in `../study_transforms.py`. Only rigid
transforms, masks and assembly are used here; no new artwork is generated.
No Unity assets, scenes, runtime scripts, project settings or gameplay changed.
