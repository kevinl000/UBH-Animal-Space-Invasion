# Scout rabbit Sniper tower

Approved direction (2026-11-08): cream rabbit, olive scout vest, small headset and exposed ears; long slim ivory/navy precision barrel with one compact scope and green accents. This visual assembly supports the README's **120-degree arc**. The operator, controls, chassis and reinforced support remain stationary; only the complete weapon and its attached muzzle rotate.

Screen revision (2026-11-08): **base-source-v2.png** adds a small olive-green monitor on the stationary support below the hinge, facing the rabbit. It supplements the joystick with a simple aiming display. The weapon, assembly coordinates, existing import metadata/GUIDs and prefab are unchanged. The superseded base v1 is archived locally outside the active art folder.

Open [preview.html](preview.html) locally to drag the aim slider or play the sweep. Positive angles correspond to Unity local Z, with the browser rotation sign reversed. [sweep-review.png](sweep-review.png) shows -60, -30, 0, +30 and +60 degrees using the same assembly coordinates. The preview sweep demonstrates reachable poses; it is not an idle-animation requirement.

## Unity assets

`Assets/Prefabs/Towers/SniperTowerVisual.prefab` is visual only:

```text
SniperTowerVisual       ground mounting origin; local +Y is outward
  Base                 stationary rabbit, chassis, controls, support and screen
  CannonPivot          independent weapon SpriteRenderer; aim this Transform
    Muzzle             emission reference; local +Y follows the bore
```

Base draws at order 0 and weapon at order 1 on the Default sorting layer, using Unity's default sprite material. Both SpriteRenderers have identity color/scale/rotation. The root and `CannonPivot` start at zero rotation. There are no gameplay scripts, colliders, Animator or aiming/clamping logic. Future gameplay must rotate `CannonPivot` within **local Z -60 to +60 degrees**, relative to the tower root's outward direction. Parent the root to the planet so that rotation carries the entire assembly.

Runtime sprites are `Assets/Art/Towers/Sniper/SniperBase.png` and `SniperWeapon.png`. Both are byte-identical to the selected source PNGs, with their original 1254 x 1254 canvases and alpha preserved. Imports use Sprite/Single, custom pivots, Full Rect mesh, Point filtering, Clamp wrapping, alpha transparency, no mipmaps, no compression and no NPOT resizing.

## Coordinates and scale

[assembly.json](assembly.json) records sources, hashes, runtime paths/GUIDs and verification. Image coordinates are pixel-edge coordinates measured from the top left, not an assumed centered pivot.

| Part | Point / setting | Value |
| --- | --- | --- |
| Base | Ground mount | (582, 1204) |
| Base | Socket center | (1091, 484) |
| Base | PPU | 1500 |
| Weapon | Hinge center | (626.5, 1070.5) |
| Weapon | Muzzle exit, at top silhouette edge on bore axis | (626.5, 58) |
| Weapon | PPU | 1800 |
| CannonPivot | Root-relative position | (0.3393333, 0.48) |
| Muzzle | Hinge-relative position | (0, 0.5625) |

The weapon's scope rotates with its barrel. Different PPU values normalize independent generated sources; original source pixels are not repainted or resized for runtime. Base visible width at scale 1 is about **0.7693 units**, close to Bomb's approximately 0.76 and Laser's 0.70 source-scale footprints. Hinge-to-muzzle distance is 0.5625 units, compared with Bomb's approximately 0.329, giving the sniper its longer silhouette. Apply the intended overall tower scale when comparing on a planet. The full swept envelope is larger than the base footprint; existing sockets and neighboring towers have not been fitted for this asset.

## Provenance

Built-in `image_gen` generated the two assets independently using actual approved references. [base.prompt-v1.txt](base.prompt-v1.txt) used `Assets/Art/Planets/UnionPrime/LaserPortrait.png` and `Art/Towers/Bomb/base-source-v2.png` for character/style and support-layout reference. [weapon.prompt-v1.txt](weapon.prompt-v1.txt) used that Laser portrait and `Art/Towers/Bomb/cannon-source-v1.png` for style, machinery and joint reference. A targeted built-in edit added the monitor to the existing v1 base using [base-screen.prompt-v2.txt](base-screen.prompt-v2.txt), preserving its layout visually and its measured socket/ground placement. Pixel-identical preservation through generation is not claimed. Current sources are **base-source-v2.png** and **weapon-source-v1.png**. Exact prompts remain as provenance; historical v1 references in prompts are not rebuild dependencies. The superseded no-screen base is preserved locally at `.git/menu-work/tower-screen-update/superseded/Sniper/base-source-v1.png` and is not part of the committed assets. No unrelated approved artwork was replaced.

## Verification and hookup limits

Run `python Art/Towers/Sniper/verify_assembly.py` from the repository root with Pillow installed to recheck source hashes/runtime equality, alpha and the full sampled arc, and rebuild the five-angle review using rigid transforms and nearest-neighbor scaling. The script does not launch Unity or modify source PNGs. Detailed angle results are local at `.git/menu-work/sniper-tower/geometry-samples.json`.

Offline geometry passed **241 angles at 0.5-degree intervals** again with the screen-equipped base, alpha >=128 and weapon sampling every four source pixels. There were zero sampled weapon/base intersections outside a **0.12-unit joint region**, positive joint contact at every angle, and zero base hits along the outgoing muzzle centerline sampled at one base-source-pixel intervals for two world units. The screen's conservative image-space bounds, recorded in the manifest, remain protected even where they approach that joint region. The exclusion is confined to the rounded breech/socket and upper support, away from the rabbit and lower chassis. These are sampled raster checks, not a continuous collision proof, and the thin centerline does not prove clearance for a projectile of arbitrary width.

Offline asset checks also verified fresh unique GUIDs, prefab sprite references/file IDs, internal hierarchy, pivots/offsets, import settings and browser JavaScript syntax. No Unity launch or Unity tests were performed.

Current `ShootingTower` spawns at the tower root and uses `fov` as a distance; it does not yet rotate a weapon, use this muzzle or enforce an angular arc. This task leaves those scripts, TowerData, scenes and shared prefabs unchanged. `TowerProjectile` currently uses local +X as its forward axis; this weapon/muzzle uses +Y, so future projectile/effect hookup must account for that convention. Sniper ammunition, muzzle effects, recoil, targeting and balance remain separate work.

Manual review for the team: inspect this prefab at neutral, both limits and intermediate angles; confirm fixed rabbit, connected hinge, scope clearance, muzzle alignment and sprite/material imports. Then check final-camera readability, terrain seating/foreground occlusion, neighboring towers and projectile-width clearance on the intended planet. Gameplay hookup must add aiming/clamping and spawn from the muzzle before live firing can be assessed.
