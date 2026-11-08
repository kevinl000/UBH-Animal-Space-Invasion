# Laser SMG rabbit tower

Direction accepted 2026-11-08: an upgraded version of the new basic Laser, with
one reinforced ivory/navy/cyan barrel, four cooling vents, an extra cyan power
cell and a small cyan headset on the familiar orange-suited rabbit. Both ears
remain exposed. The rabbit, controls, monitor, chassis and support stay fixed;
the whole weapon and its muzzle rotate independently through local Z -60 to +60
degrees. Neutral fires local +Y, outward from the planet.

Open [preview.html](preview.html) locally for the aiming slider, sweep and muzzle
markers. [sweep-review.png](sweep-review.png) shows five poses using the same
coordinates. The sweep illustrates reachable poses; it is not an idle animation.

## Current artwork and Unity assets

- Stationary operator/base: **base-source-v3.png**. The v2 edit lowered the monitor
  so the wider SMG clears it at the left aiming limit. The v3 edit matches the
  other rabbits' focused eyes/brows, small square nose and tiny coral mouth,
  using the Sniper as the expression reference. The cyan headset and lower-screen
  layout remain. Both earlier sources are preserved beside it.
- Rotating weapon: **weapon-source-v1.png**.
- Runtime copies: `Assets/Art/Towers/LaserSMG/LaserSMGBase.png` and
  `LaserSMGWeapon.png`, byte-identical to the selected source PNGs.
- Visual prefab: `Assets/Prefabs/Towers/LaserSMGTowerVisual.prefab`.

```text
LaserSMGTowerVisual     ground mounting origin; local +Y outward
  Base                 fixed rabbit, controls, screen, chassis and support
  CannonPivot          independently rotating weapon; SpriteRenderer order 1
    Muzzle             bore/emission reference, local +Y along the weapon
```

Base draws at order 0 on the Default sorting layer. All transforms start with
identity scale/rotation. Imports use Sprite/Single, Point filtering, Clamp wrap,
custom pivots, Full Rect mesh, alpha transparency, no mipmaps/compression and no
NPOT resizing. No gameplay scripts, colliders or Animator are attached.

## Assembly

[assembly.json](assembly.json) records source/runtime hashes, GUIDs, scale,
coordinates and verification. Pixel coordinates are measured from the top left
at pixel edges; the imported pivots are the actual mounting/hinge anchors.

| Setting | Value |
| --- | --- |
| Base canvas and PPU | 1278 x 1230; 1000 PPU |
| Base ground mount | (598, 1101) |
| Base socket center | (903, 604) |
| Weapon canvas and PPU | 1254 x 1254; 3000 PPU |
| Weapon hinge center | (627, 1011) |
| Muzzle outer top edge, on bore axis | (627, 201) |
| CannonPivot local position | (0.305, 0.497, 0) |
| Muzzle local position under CannonPivot | (0, 0.27, 0) |

Different PPU values normalize the independently generated images without
resizing or repainting their source pixels. The base's visible width is about
0.718 units, close to the basic Laser's 0.70. The weapon's visible width is about
0.183 units, versus about 0.135 for the latest uniformly exported basic Laser
weapon; hinge-to-muzzle length is 0.27 versus 0.25. These values describe the
art assemblies at scale 1. Planet mounting scale and final seating are separate.

## Provenance

Built-in `image_gen` used the actual new basic Laser sources as edit/reference
inputs: `Art/Towers/Laser/Redesign/Sweep/base-generated-v2.png` and
`gun-generated-v2.png`. Reference hashes are retained in the manifest. Their
approved artwork was not modified by this task. Exact prompts are
[base.prompt-v1.txt](base.prompt-v1.txt),
[weapon.prompt-v1.txt](weapon.prompt-v1.txt) and
[base-screen.prompt-v2.txt](base-screen.prompt-v2.txt), followed by
[base-expression.prompt-v3.txt](base-expression.prompt-v3.txt). The second base edit
removed the old monitor and placed it lower on the support; the requested
position was approximate, and the final measured monitor bounds are recorded
in the manifest. The expression edit targets that v2 base and uses the Sniper's
base only as a facial reference. Runtime PNG bytes match the selected v3 source;
PPU, anchors, weapon, prefab and all Unity metadata are unchanged by this edit.
Pixel-identical preservation through generation is not claimed.

## Offline verification and remaining hookup

Run `python Art/Towers/LaserSMG/verify_assembly.py` from the repository root with
Pillow installed. It checks source/runtime equality, real alpha, import settings,
pivots, GUID uniqueness, prefab references/hierarchy and preview coordinates,
then samples the full aiming arc and regenerates the five-angle review using
only rigid transforms and nearest-neighbor scaling. Detailed angle results are
local at `.git/menu-work/laser-smg/geometry-samples.json`.

The final assembly passed **241 angles at 0.5-degree intervals**, with weapon
sampling every four source pixels and alpha >=128: zero sampled base overlaps
outside a **0.09-unit joint region**, positive joint contact at every angle and
zero base hits along a two-world-unit outgoing muzzle centerline. The monitor is
always protected, including inside the joint exclusion. The exclusion covers
only the socket/rounded breech and adjacent upper support. These are sampled
raster checks, not continuous collision proofs or projectile-width clearance.
The browser's inline JavaScript also passed Node's syntax check; a browser
interaction check was not run.

The README row remains a design reference: level 2, HP 30, cost 10, fuel -3/s,
FOV 120 degrees, damage 2, single-target, faster laser projectiles. No numerical
firing interval was provided or chosen. No TowerData or gameplay was changed.
Current `ShootingTower` spawns at its root and uses `fov` as distance; it does not
yet aim this weapon, enforce the angular arc or spawn from this muzzle.
`TowerProjectile` faces local +X, whereas the weapon/muzzle points +Y. Future
hookup must account for that convention. The existing RabbitLaser effects can
be considered during that hookup; no new projectile/effect assets are supplied.

No Unity launch or tests were performed. Team manual review: inspect the prefab
at neutral, both limits and intermediate angles; check fixed operator, connected
hinge, monitor clearance, muzzle alignment and material/import appearance. Then
check final-camera readability, terrain/foreground seating, neighboring towers
and real projectile-width clearance before connecting targeting and rapid fire.
