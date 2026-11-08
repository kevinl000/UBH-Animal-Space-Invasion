# Rabbit Laser shot

Approved direction: a short cyan bolt with a white core, two-frame flight pulse, brief forward muzzle flash, and small single-target impact spark. Built-in image generation used `Assets/Art/Planets/UnionPrime/LaserPortrait.png` as the style/color reference. The exact prompt is in `prompt-v1.txt`; the original RGBA sheet is preserved in `rabbit-laser-sheet-source-v1.png`. The Unity sheet is a byte-identical copy. `sprites.json` records the seven slices, pivots, timing and muzzle reference coordinates.

## Assets

Expand `Assets/Art/Projectiles/RabbitLaser/RabbitLaserSheet.png` for seven sprites: `RabbitLaserFlight01/02`, `RabbitLaserMuzzle01/02`, and `RabbitLaserImpact01/02/03`. The two empty cells in the generated 3 by 3 sheet are not imported as sprites. Each populated cell is 418 by 418 pixels.

| Visual prefab under `Assets/Prefabs/Projectiles/` | Animation under `Assets/Animations/Projectiles/` | Behavior |
| --- | --- | --- |
| `RabbitLaserFlightVisual.prefab` | `RabbitLaserFlight.anim` and `.controller` | Two sprite frames at 12 fps; loops every 0.167 seconds. |
| `RabbitLaserMuzzleVisual.prefab` | `RabbitLaserMuzzle.anim` and `.controller` | Two frames at 24 fps; empty sprite after 0.083 seconds, no loop. |
| `RabbitLaserImpactVisual.prefab` | `RabbitLaserImpact.anim` and `.controller` | Three frames at 18 fps; empty sprite after 0.167 seconds, no loop. |

Each prefab contains only Transform, SpriteRenderer and Animator. All clips animate only the renderer's `m_Sprite`; they contain no movement, rotation, scale, color curves, animation events or root motion. Use the supplied controllers or incorporate the clips into the gameplay animation setup later. One-shot effects end blank but their GameObjects remain for gameplay to clean up or reuse; replay/reinitialization on pool reuse belongs to that hookup.

The sprite importer uses Point filtering, alpha transparency, Clamp wrapping, no mipmaps/compression, Full Rect meshes, custom pivots and 1024 pixels per unit. Default sorting is order 10 on the Default layer. Bolt length is approximately 0.234–0.252 world units and thickness 0.051–0.052 at scale 1; the flash is approximately 0.121 units long initially. These are starting visual sizes relative to the existing 24 by 33 gun sprite at 100 PPU. Gameplay camera readability and final tower scaling still require manual review.

## Barrel attachment and homing

`Assets/Scripts/Unique Towers/TowerProjectile.cs` accepts the barrel's world spawn position in `Launch`, updates direction toward a living target in `FixedUpdate`, and rotates the projectile so **local +X** faces that target. These sprites point +X. Put the flight visual under the gameplay projectile root with local position/rotation zero; let the existing projectile code own movement and rotation. The pulse simply continues during both straight and turning flight. No baked orbit path, angle-specific frames, continuous beam, or long straight trail is included.

The flight sprite and muzzle flash use **rear-edge pivots**, aligned separately per frame to compensate for offsets in the generated sheet. Their visible bodies extend forward from the origin, allowing the bolt to emerge in front of the barrel rather than being centered inside it. The visual bounds are measured at alpha >=16; very faint generated edge pixels can extend slightly behind that anchor. Future projectile colliders should be fitted to the visible bolt with their center forward of this origin, rather than fitted to the full transparent cell. The flight pulse has a small natural length change between its two frames.

Attach the muzzle-flash visual at a muzzle marker on the **rotating gun**, so it inherits the gun's aim while the rabbit/base stays fixed. Its +X must point along the gun's firing axis. The accepted tower artwork points **local +Y** in its neutral pose, so a muzzle visual parented directly to that gun needs **local Z rotation +90 degrees**. If a future muzzle marker already points its own +X outward, the flash should instead have identity local rotation.

The accepted gun reference is `Art/Towers/Laser/Redesign/Sweep/gun-sprite.png` (24 by 33). Its muzzle is **(12, 3)** in pixel-edge coordinates from the top left, and its intended hinge is **(12, 28)**. This puts the muzzle 25 source pixels above the hinge. Convert using the actual gun pivot and scale when setting up gameplay: the imported `LaserGun.png` asset currently has a centered sprite pivot, whereas the menu's RectTransform assembly sets its gun pivot at the hinge. The reference offset is not a ready-made gameplay Transform position for every assembly. Tower artwork, its pivots and the menu assembly were left unchanged.

These are visual prefabs, not `TowerProjectile` gameplay prefabs and cannot be assigned directly to the typed `TowerData.projectilePrefab` field. Firing, muzzle spawn/trigger, impact spawn/trigger, colliders, pooling and cleanup remain for the gameplay hookup. No runtime scripts, tower/enemy stats, scenes or existing prefabs were changed.

## Manual review

No Unity tests, imports or validation runs were executed, per `AGENTS.md`. Offline checks cover transparency, source/runtime equality, slice bounds, emission/contact pivots, GUID/file-ID references, frame order, loop flags and the absence of Transform animation.

In Unity, expand the sheet to inspect all seven slices and preview each clip on its visual prefab. Check that the muzzle flare starts at the barrel exit at neutral and rotated gun angles, and that the bolt starts ahead of the barrel. After gameplay hookup, check stationary and orbiting targets: the pulse must follow the projectile root's current heading without snapping to a recorded path, the barrel flash must follow the gun, and the impact must remain a small contact spark. Confirm size, sorting and pooled effect replay through the gameplay camera.
