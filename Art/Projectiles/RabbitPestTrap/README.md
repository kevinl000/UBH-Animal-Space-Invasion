# Pest Trap amber resin shot

Approved direction: a compact amber sticky-resin glob matching the launcher's reservoir, a tiny nozzle spurt and a small sticky hit splat on one enemy. No flame, gas, wide splash, ground puddle or radius-damage cue. The glob has a rounded leading bulb and short rear taper; gameplay code supplies movement and turning.

Open [preview.html](preview.html) locally to aim the existing Pest Trap tower through its +/-60-degree arc and preview a shot. It uses actual source sprites, pivots and prefab scales. Travel distance/speed are illustrative, not gameplay configuration. Frame cards are enlarged for inspection.

## Sources and import

Built-in `image_gen` created [rabbit-pest-trap-sheet-source-v1.png](rabbit-pest-trap-sheet-source-v1.png) using `Art/Towers/PestTrap/launcher-source-v1.png` as the resin palette/pixel-style reference. [prompt-v1.txt](prompt-v1.txt) records the exact prompt and mode. `Assets/Art/Projectiles/RabbitPestTrap/RabbitPestTrapSheet.png` is a byte-identical copy; no source pixels were repainted or resized.

[sprites.json](sprites.json) records source hash, texture GUID, slices, pivots, scales and animation timing/GUIDs. Six sprites occupy a 1254 x 1254 sheet with 418 x 418 cells. Top-middle, top-right and middle-right cells are not imported. Faint generated edge residue is preserved; bounds/padding measurements use alpha >=16, with strong bounds also recorded at alpha >=128. Unused cells contain alpha <=1 residue.

Import settings: Sprite/Multiple, custom slice pivots, **2048 PPU**, Point filtering, Clamp wrapping, alpha transparency, Full Rect meshes, no mipmaps/compression and no NPOT resizing. All prefabs use Unity's default sprite material and sorting order 10 on the Default layer.

## Visual assets

Prefab paths below are under `Assets/Prefabs/Projectiles/`; matching clips/controllers are under `Assets/Animations/Projectiles/`.

| Prefab | Sprites | Behavior |
| --- | --- | --- |
| `RabbitPestTrapProjectileVisual.prefab` | `RabbitPestTrapFlight` | Static Transform + SpriteRenderer; no Animator. XY scale **0.6** fits the nozzle. |
| `RabbitPestTrapMuzzleVisual.prefab` | `RabbitPestTrapMuzzle01/02` | `RabbitPestTrapMuzzle.anim` / `.controller`: two-frame spurt at 24 fps; blank after 0.0833 seconds, non-looping. |
| `RabbitPestTrapImpactVisual.prefab` | `RabbitPestTrapImpact01/02/03` | `RabbitPestTrapImpact.anim` / `.controller`: dab, spreading splat, fading patch at 12 fps; blank after 0.25 seconds, non-looping. |

Spurt and splat have identity scale and add only an Animator. Clips swap only `SpriteRenderer.m_Sprite`, with no Transform/color curves, root motion or animation events. The final blank key hides the sprite; gameplay still owns cleanup, pooling and replay/reinitialization.

At supplied scales, the glob measures approximately **0.0580 units long by 0.0308 thick** at alpha >=16. The nozzle opening at source row 270 measures 67 pixels / 2100 weapon PPU, about **0.0319 units** wide. The initial spurt is about 0.0649 by 0.0303 units; the largest splat about 0.0986 by 0.0669. These are art measurements, not collider tolerances or damage areas. Apply any overall tower scale consistently and inspect through the final camera.

## Attachment and gameplay

Flight and spurt point **local +X**, matching `TowerProjectile`. Their rear/emission pivots align each frame's visible left edge and centerline, so the body extends forward at alpha >=16. Very faint pixels can extend beyond measured bounds. Impact frames have separately measured contact pivots compensating for generated offsets.

Place the flight visual under the gameplay projectile root at local position/rotation zero, preserving its **0.6 XY scale**. Let the root own flight and homing. Fit future colliders to the visible body forward of the rear pivot, not the transparent cell.

Attach the spurt at `CannonPivot/Muzzle` with zero local position and **local Z +90 degrees**: effect +X then follows marker +Y. The marker is source point `(627, 216)`, local `(0, 0.383571429)` from the hinge at weapon PPU 2100. It follows the moving launcher while the rabbit/screen stay fixed. Emit the gameplay projectile at the marker's world position using the projectile root's +X facing convention.

Spawn the splat at the actual enemy contact point. Its quarter-second fade is a hit effect, not a slow/DOT timer or persistent coating. Following a moving enemy during the effect, replay, cleanup and any longer-lived status visual belong to gameplay hookup.

Current `PestTrapTower.OnHitEnemy` applies a three-second slow at factor 0.5 and one immediate damage tick; it does not schedule the full three-second DOT proposed in the root README. Current `ShootingTower` spawns at the tower root, compares `fov` as a distance and does not aim/clamp the launcher. This work changes none of those scripts/values. Visual prefabs have no `TowerProjectile` component and cannot directly fill `TowerData.projectilePrefab`.

## Checks and manual review

Offline checks passed: real alpha, source/runtime equality, six padded slices, pivots, unique GUIDs, sprite/clip/controller/prefab references, visual-only scope, frame order/timing, non-loop flags and final blank keys. The browser preview loads the actual local images and passes JavaScript syntax checking. Detailed local measurements are at `.git/menu-work/rabbit-pest-trap/offline-validation.json`.

The straight outgoing corridor was sampled at **241 angles**, every 0.5 degrees from -60 to +60, for two units beyond the muzzle. Forward spacing is one base-source pixel, with five lateral samples spanning the glob's full visible thickness at 0.6 scale. No sample intersected the fixed rabbit/base/screen at alpha >=128. These are sampled raster checks, not continuous proofs or validation of later homing, terrain or neighbors. No Unity launch, import validation or Unity tests were run.

Manual Unity review: inspect all six slices and both clips. Check nozzle alignment and glob width at neutral, both limits and intermediate angles. Review camera readability, material/sorting, enemy contact placement and replay/cleanup after gameplay hookup. DOT behavior needs its own implementation review.
