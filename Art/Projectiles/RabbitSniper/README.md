# Rabbit Sniper round, muzzle flash and impact

Approved design: one physical precision round with a pointed ivory nose, navy body and muted olive band; a brief warm-white muzzle flash; a compact single-target contact spark. No flight pulse, long trail, continuous beam or area-explosion cue. The existing sniper tower and gameplay scripts remain unchanged.

Open [preview.html](preview.html) locally to aim the existing tower through its +/-60-degree arc and preview a shot. It uses the actual source sprites, measured pivots and Unity relative sizes. The preview's straight travel distance/speed are illustrative and do not configure gameplay. Its individual frame cards are enlarged for inspection.

## Sources and runtime sheet

Built-in `image_gen` created [rabbit-sniper-sheet-source-v1.png](rabbit-sniper-sheet-source-v1.png) using `Art/Towers/Sniper/weapon-source-v1.png` as the palette/style reference. [prompt-v1.txt](prompt-v1.txt) records the exact prompt and mode. `Assets/Art/Projectiles/RabbitSniper/RabbitSniperSheet.png` is a byte-identical copy of that original RGBA sheet; no source pixels were repainted or resized.

[sprites.json](sprites.json) records the source hash, texture GUID, all slices/anchors, frame timing, effect controller/clip GUIDs and facing conventions. Six sprites occupy a 1254 x 1254 sheet with 418 x 418 cells. Top-middle, top-right and middle-right cells are not imported. Very faint generated alpha <=1 residue in those unused cells is preserved; visible bounds and padding measurements use alpha >=16, with strong bounds also recorded at alpha >=128.

The importer uses Sprite/Multiple, custom per-slice pivots, **2048 PPU**, Point filtering, Clamp wrapping, Full Rect meshes, alpha transparency, no mipmaps, no compression and no NPOT resizing. All three visual prefabs use identity scale/rotation and sorting order 10 on the Default layer, with Unity's default sprite material.

## Visual prefabs and clips

All paths below are under `Assets/Prefabs/Projectiles/`. Clips/controllers are under `Assets/Animations/Projectiles/`.

| Prefab | Sprites | Behavior |
| --- | --- | --- |
| `RabbitSniperProjectileVisual.prefab` | `RabbitSniperFlight` | Static Transform + SpriteRenderer; no Animator needed. |
| `RabbitSniperMuzzleVisual.prefab` | `RabbitSniperMuzzle01/02` | `RabbitSniperMuzzle.anim` / `.controller`: two frames at 24 fps, blank after 0.0833 seconds, non-looping. |
| `RabbitSniperImpactVisual.prefab` | `RabbitSniperImpact01/02/03` | `RabbitSniperImpact.anim` / `.controller`: three frames at 18 fps, blank after 0.1667 seconds, non-looping. |

Effects add only an Animator. Both clips swap `SpriteRenderer.m_Sprite` exclusively, with no Transform/color curves, animation events or root motion. Their final empty key hides the sprite; it does not destroy/despawn the object. Playback, pool reuse/reinitialization and cleanup belong to future gameplay hookup.

At scale 1, the round's visible alpha >=16 bounds are about **0.1133 units long by 0.0269 units thick**. The barrel opening measured along source row 110 is 51 pixels / 1800 PPU, about **0.0283 units** wide. This is a visual fit measurement, not a collider tolerance. The initial muzzle flare is about 0.0762 units long by 0.0522 high; the largest hit frame is about 0.0566 units wide. These are starting art sizes relative to the tower's unit-scale assembly. Match any overall tower scale at hookup and inspect through the final camera.

## Pivots and attachment

The flight round and muzzle flashes point local **+X**. Each uses a rear/emission pivot at its own visible left edge, with its centerline at local Y=0. The separately measured pivots compensate for sheet offsets between frames. At alpha >=16, the round and flash extend forward from their origins; very faint pixels can extend outside the measured bounds. Impacts use contact pivots: the first two align the bright central glint, and the last centers its remaining flecks around the same contact position.

Current `TowerProjectile` faces its root's local +X toward a target and can home during flight. Parent the projectile visual under that gameplay root at local position/rotation zero; let the code own motion and heading. No straight-only or hitscan behavior is implemented by this artwork.

The existing `SniperTowerVisual/CannonPivot/Muzzle` marker points local **+Y** out of the bore. Attach the muzzle effect at that marker with zero local position and **local Z +90 degrees**, so its +X aligns with the marker's +Y. The marker is `(0, 0.5625)` from the weapon hinge, measured at source image point `(626.5, 58)` using weapon PPU 1800. It follows the rotating weapon independently of the fixed rabbit/screen.

Spawn the flight visual/gameplay projectile at the muzzle's world position and face the gameplay root toward the target using its own +X convention. Spawn a separate impact at the actual contact point. Fit future projectile colliders to the visible body forward of the rear pivot, not to the full transparent cell.

Current `ShootingTower` still spawns at the tower root and does not use this muzzle or rotate/clamp the weapon. These prefabs have no `TowerProjectile` component and cannot directly fill `TowerData.projectilePrefab`. Firing, angular targeting, projectile speed, damage, hit spawning, pooling, sound and recoil remain separate implementation work.

## Checks and manual review

Offline checks passed for real alpha, source/runtime equality, six padded slices, emission/contact pivots, unique GUIDs, native sprite/clip/controller references, visual-only component scope, exact frame order/timing, non-loop flags and final empty keys. The browser preview loaded correctly in headless Chrome and its JavaScript passed a syntax check. Local detailed checks are at `.git/menu-work/rabbit-sniper/offline-validation.json`.

The straight outgoing corridor was sampled at **241 aim angles**, every 0.5 degrees from -60 to +60, for two units beyond the muzzle, with one base-source-pixel forward spacing and five lateral samples spanning the round's full visible thickness. No sample intersected the stationary rabbit/base/screen at alpha >=128. This is sampled geometry, not a continuous proof; it does not validate later homing, terrain or nearby towers. No Unity launch, import validation or Unity tests were run.

Manual Unity review: inspect the six slices and the two one-shot clips. Check the muzzle flare at neutral, both limits and intermediate aim angles; confirm that the round starts ahead of the lip and the impact stays at contact. Check camera readability, sorting/materials and projectile width, then effect replay/cleanup and turning toward orbiting targets after gameplay hookup.
