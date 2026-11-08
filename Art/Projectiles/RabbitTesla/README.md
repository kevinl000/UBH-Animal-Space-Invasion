# Tesla electric bolt, electrode flash and AoE burst

Approved output: a compact traveling cyan-white crackling bolt, a brief flash at the coil electrode, and a localized electrical burst that damages **every eligible enemy inside its affected area**. The 120-degree sweep is the tower's aiming arc, separate from the impact radius. No capped chain, stun, freeze, persistent field or damage-over-time behavior is implied.

Open [preview.html](preview.html) locally to aim the existing Tesla tower, fire, repeat shots and change an illustrative burst radius. Target markers inside the radius flash together on impact; markers outside stay unchanged. The example markers, radius, travel distance and flight timing configure only this art preview. [frame-review.png](frame-review.png) compares all eight sprite slices.

## Unity visual assets

Prefab paths below are under `Assets/Prefabs/Projectiles/`. Each contains Transform, SpriteRenderer and Animator only, using the Default sorting layer, order 10 and Unity's default sprite material.

| Prefab | Animation | Absolute uniform XY scale |
| --- | --- | --- |
| `RabbitTeslaFlightVisual.prefab` | Two-frame crackle at 12 fps; loops every 0.1667 seconds | 0.25 |
| `RabbitTeslaMuzzleVisual.prefab` | Two-frame electrode flash at 24 fps; ends empty at 0.0833 seconds | 0.35 |
| `RabbitTeslaBurstVisual.prefab` | Four frames at 16 fps: ignition, opening ring, peak ring, fading fragments; ends empty at 0.25 seconds | 5.658019885743, representing reference radius 1 |

Matching `.anim` and `.controller` files live under `Assets/Animations/Projectiles/`. Clips change only `SpriteRenderer.m_Sprite`; there are no Transform curves, root motion or events. One-shot objects remain alive after their renderer becomes empty. Gameplay owns replay, pooling and cleanup. The flight pulse does not implement movement or targeting.

## Axes and hookup later

The flight bolt and electrode flash point **local +X**, with per-frame rear emission pivots. The existing `TowerProjectile.cs` also faces +X and tracks living targets; parent the flight visual under a gameplay projectile root at identity local position/rotation. The supplied scale makes the bolt approximately 0.066-0.068 units long, sized to clear the operator throughout the aiming arc. Fit future colliders to the visible bolt ahead of its rear pivot, not the full transparent slice.

The tower's `CoilPivot/Emission` marker points **local +Y** out of the electrode. Parent the flash there and rotate its visual **local Z +90 degrees**, converting its +X to that +Y. Spawn the independent traveling projectile at the marker's world position. `ShootingTower.Fire()` currently spawns at the tower root; this art work does not change it or implement the +/-60-degree aiming clamp.

Spawn the burst at the actual impact/blast center in world space, independent of the moving/despawned projectile. These prefabs have no `TowerProjectile` component and cannot be assigned directly to the typed `TowerData.projectilePrefab` field. Firing, collisions, hit-set calculation and triggering effects remain gameplay work.

## Calibrating the burst radius

The actual Tesla radius remains **unassigned**. Current `AoETower.effectRadius` defaults to 2 world units, but that is not an approved Tesla balance value. Do not infer a damage radius from 120 degrees or the preview slider.

The maximum visible radial extent across the four frames is **180.982043308 source pixels**, measured from each corrected slice pivot using pixel centers at alpha >=16. At 1024 PPU, use this absolute uniform world-space scale after the gameplay radius is confirmed:

```text
visualScale = actualEffectRadiusWorld * 1024 / 180.982043308
```

The prefab's supplied reference scale is 5.658019885743 for radius 1. Multiply that reference scale by the actual radius when the parent has unit world scale; otherwise compensate for inherited scale. Do not additionally multiply by the tower art's scale, because the damage query uses world-space distances. Avoid nonuniform stretching.

This is cosmetic calibration of an irregular burst, not an exact circular boundary or damage query. Open gaps and interior transparency keep enemies visible. Smaller frames occupy less of the area; faint pixels below the measurement threshold may extend farther. Use the actual radius query to determine all recipients, rather than a fixed count or the sprite's silhouette.

## Source and import

Built-in `image_gen` generated v1 using the approved Tesla emitter as its palette/style reference. The initial peak burst crossed its cell boundary; a targeted edit corrected its framing and removed interior haze. **`rabbit-tesla-sheet-source-v2.png` is current**. The original v1 remains source history and is not imported. [prompt-v1.txt](prompt-v1.txt) and [correct-gutters.prompt-v2.txt](correct-gutters.prompt-v2.txt) preserve exact prompts and generation modes.

`Assets/Art/Projectiles/RabbitTesla/RabbitTeslaSheet.png` is a byte-identical copy of v2, with original alpha and pixels preserved. No source artwork was repainted, resized or trimmed. The sheet is 1254 x 1254, sliced into eight 418 x 418 cells. Bottom-right is unused and not imported. The minimum measured alpha>=16 slice padding is 18 pixels. Per-frame custom pivots compensate for generated offsets; do not replace them with centered pivots.

Imports use Sprite/Multiple, 1024 PPU, Point filtering, Clamp wrapping, Full Rect, real alpha transparency, no mipmaps/compression and no NPOT resizing. [sprites.json](sprites.json) records source hash, slices, pivots, GUIDs, animation keys, prefab scales and radius measurements.

## Verification and manual review

Run `python Art/Projectiles/RabbitTesla/verify_assets.py` with Pillow and Node for offline checks and a rebuilt frame review. It checks source/runtime equality, alpha, slice padding, empty cell, pivots, radius measurements, transparent ring centers, native sprite/clip/controller/prefab references, 11 unique new asset GUIDs, frame order, loop/blank endings, absence of Transform animation and local preview references/JavaScript syntax.

The full-width outgoing corridor cleared the base at **241 angles**, every 0.5 degrees from -60 to +60, for two world units beyond the electrode. The conservative width is **0.02978515625 units**, sampled at five lateral offsets against base alpha>=128. The initial wider effect grazed an ear at the limit; the final visual scales resolve the sampled overlap. This is a sampled straight launch check, not a continuous proof or validation of later homing motion, terrain or neighboring entities.

The local preview was rendered and visually inspected in headless Chrome. **No Unity launch or Unity tests were performed.** In Unity, inspect all slices/clips and compare bolt/flash placement at neutral, intermediate and endpoint angles. Confirm final camera readability, material/sorting and planet/neighbor clearance. After gameplay hookup, test actual inside/outside-radius enemies, moving targets, flash attachment, effect replay and pooled cleanup.
