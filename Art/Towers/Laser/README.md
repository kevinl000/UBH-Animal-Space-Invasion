# Rabbit-operated Laser cannon

Current revision (2026-11-08): the orange-clad cream rabbit has a small **cyan operating screen** fixed low on the diagonal support, and the moving weapon is a proper **single laser cannon** with an ivory/navy shroud, recessed cyan emitter, two cooling slots and a cyan charge inset. The rabbit, joystick, seat, chassis and screen stay fixed. The cannon aims through **local Z -60 to +60 degrees**, relative to local +Y/outward.

Expression update (2026-11-08): the rabbit now shares the other tower operators'
focused eyes/brows, small square nose and tiny coral mouth, using the Sniper as
the expression reference. The latest lower-screen layout, outfit, export
transforms, weapon, muzzle and Unity metadata are retained. The prior sources
remain available; the current base is **base-generated-v5.png**.

- [Interactive aiming preview](Redesign/Sweep/preview.html)
- [Five-angle comparison](Redesign/Sweep/sweep-review.png) and [animated sweep](Redesign/Sweep/sweep.gif)
- [Base/operator](Redesign/Sweep/base-sprite.png), [cannon](Redesign/Sweep/gun-sprite.png) and [portrait](Redesign/Sweep/portrait-v3.png)
- [Current sources, export transforms, runtime paths and pivots](Redesign/Sweep/assembly.json)

## Unity assets and compatibility

The existing `Assets/Art/Planets/UnionPrime/LaserBase.png`, `LaserGun.png` and `LaserPortrait.png` are updated at their existing paths, preserving all three `.meta` files and GUIDs. Base remains **70 x 72**, gun **24 x 33**, portrait **76 x 76**. Base/gun remain 100 PPU with centered import pivots. Existing menu/planet consumers therefore retain their canvas sizes and rotation/ground coordinates. Scene files, planet prefabs, terrain, gameplay scripts and projectile artwork are unchanged.

`Assets/Prefabs/Towers/LaserTowerVisual.prefab` is a new SpriteRenderer-only assembly:

```text
LaserTowerVisual          ground mount; local +Y outward
  Base                   centered legacy sprite, fixed rabbit/screen
  CannonPivot            rotate this for aiming
    Gun                  centered legacy sprite, offset to its hinge
    Muzzle               +Y emission marker
```

The extra `Gun` child preserves the existing centered importer pivot while putting the actual rotation at the hinge. Both SpriteRenderers use Unity's default sprite material, Default sorting layer and orders 0/1. No Animator, aiming script, collider or gameplay component is added. Gameplay must select targets, enforce the +/-60-degree clamp and trigger the existing projectile/effects.

| Setting | Value |
| --- | --- |
| Base ground mount, top-left pixel-edge coordinates | (34, 71) |
| Base socket | (64, 24) |
| Gun hinge | (12, 28) |
| Current muzzle exit | **(14.5, 0)** |
| CannonPivot root-relative position | (0.30, 0.47) |
| Base renderer root-relative position | (0.01, 0.35) |
| Gun renderer relative to CannonPivot | (0, 0.115) |
| Muzzle relative to CannonPivot | **(0.025, 0.28)** |

The barrel has a small outward offset from its trunnion and uses the old canvas's top padding. This clears the rabbit's ears at the left aiming limit with the existing laser bolt's full width. The original foundation and rotation pivot remain fixed. The old muzzle `(12, 3)` is superseded. When attaching the existing +X laser flash at this +Y marker, use local Z +90 degrees. Spawn the gameplay projectile at the marker's world position and let its root own +X heading/homing.

## Sources and rebuild

Current raw sources are **`base-generated-v5.png`** and **`gun-generated-v3.png`** under `Redesign/Sweep/`. Built-in `image_gen` edited the original base to add the screen, then lowered it for shroud clearance. It generated the approved single cannon using the original Laser portrait and Bomb cannon as style references, then shifted its barrel outward while retaining the lower hinge. The targeted expression edit uses the latest lower-screen base v3 as its edit target and the Sniper base as its facial reference. Exact prompts are `base-screen.prompt-v2.txt`, `base-screen-lower.prompt-v3.txt`, `gun-cannon.prompt-v2.txt`, `gun-offset.prompt-v3.txt` and `base-expression.prompt-v5.txt`. Original raw sources and intermediate versions are retained separately; pixel-identical preservation through generation is not claimed.

`assembly.json` documents uniform nearest-neighbor scaling and translation into the existing export canvases. No source artwork is repainted by the exporter. Run `python Art/Towers/Laser/Redesign/Sweep/build_sweep.py` with Pillow to rebuild the split sprites and offline studies. It reads current source selections from that manifest. Runtime textures match the exported sprites/portrait byte-for-byte. The portrait keeps its fixed canvas and aligns the base at `(0, 4)` and gun at `(52, 0)`.

## Verification and limits

Offline checks passed at **241 angles**, every 0.5 degrees: zero opaque gun/base overlaps outside the intended 12-pixel joint region, positive hinge contact, and zero muzzle-centerline hits. The screen stays protected even inside that region. A conservative **7-pixel-wide outgoing corridor** also clears the base/operator at all poses; it covers the current approximately 5.2-pixel-wide laser bolt at source scale.

The existing twelve-site Union Prime study also passed **2,892 site/angle combinations** for moving gun and thin ray against foreground terrain. Those studies use the historical 100% tower with its 16-source-pixel outward offset; the accepted planet pipeline uses a separate 150% fit. This does not revalidate that final fit or other planets. Only the Laser's own study renders were refreshed; unrelated planet art/manifests were not rebuilt. Their historical screenshots/hashes may still describe the previous tower.

These are sampled raster checks, not continuous collision proofs. Existing texture metadata and prefab references were checked offline. No Unity launch or tests were run. In Unity, inspect neutral, both limits and intermediate poses; confirm the fixed rabbit/screen, hinge contact, muzzle alignment, material/sorting and camera readability. Recheck actual planet seating/foreground occlusion, neighboring towers, projectile width and effect replay during gameplay hookup.
