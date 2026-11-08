# Bomber Rabbit shell, muzzle flash and explosion

Approved design: one compact navy capsule shell with an amber band and short tail fins, a brief amber muzzle flare, and a yellow-orange radial burst that opens into sparse smoke. The explosion represents enemies inside a distance radius, separate from the Skunk Bomb's angular spread.

Built-in image generation used `Art/Towers/Bomb/cannon-source-v1.png` as the palette/style reference. `prompt-v1.txt` records the exact prompt; `rabbit-bomb-sheet-source-v1.png` preserves the original RGBA sheet. `Assets/Art/Projectiles/RabbitBomb/RabbitBombSheet.png` is a byte-identical runtime copy. `sprites.json` records slices, anchors, timing and radius measurements. No source pixels were repainted.

## Assets

Expand the runtime sheet to access eight sprites: `RabbitBombFlight`, `RabbitBombMuzzle01/02` and `RabbitBombExplosion01/02/03/04/05`. The empty top-right cell is not imported. Each cell is 418 by 418 pixels; pivots compensate for generated offsets. Flight and muzzle use rear emission pivots; explosions use a blast-center pivot. All import at 1024 PPU with Point filtering, Clamp wrapping, alpha transparency, no compression/mipmaps and Full Rect meshes.

| Prefab under `Assets/Prefabs/Projectiles/` | Behavior |
| --- | --- |
| `RabbitBombProjectileVisual.prefab` | Static shell, forward local +X, uniform XY scale 0.65. Visible thickness approximately 0.066 units at this scale, sized below the cannon's visible bore width. |
| `RabbitBombMuzzleVisual.prefab` | Two frames at 24 fps; ends empty after 0.083 seconds. XY scale 0.65. |
| `RabbitBombExplosionVisual.prefab` | Five frames at 18 fps; ends empty after 0.278 seconds. Default scale normalizes the maximum visible radial extent to approximately one world unit. |

The muzzle and explosion have matching `.anim` and `.controller` assets under `Assets/Animations/Projectiles/`. Their clips are non-looping and animate only `SpriteRenderer.m_Sprite`, with no Transform keys, root motion or animation events. Their objects remain alive after the renderer becomes empty; gameplay must handle replay and cleanup. Both use Transform, SpriteRenderer and Animator; the static shell uses only Transform and SpriteRenderer. Sorting is order 10 on the Default layer using Unity's default sprite material.

## Cannon and projectile hookup later

The current `TowerProjectile` updates its homing direction and rotates its root so local **+X** is forward. Place this shell visual at local position/rotation zero under that gameplay root. Its rear pivot keeps the shell ahead of the spawn point; fit future colliders to the visible shell with a forward center offset, not to the full transparent cell. No baked arc, rotation animation, rocket exhaust or trail is included.

The Bomb tower's `CannonPivot/Muzzle` marker instead points **+Y** along the bore. Parent the muzzle visual there and set its local Z rotation to **+90 degrees** so the flash's +X matches that outward +Y. It should remain attached to the rotating cannon while briefly visible. Spawn the independent shell from the muzzle's world position when gameplay wiring is added. The current `ShootingTower.Fire` passes the tower root position; this art task does not replace that with the muzzle marker.

These visual prefabs do not contain `TowerProjectile` and cannot be assigned directly to the typed `TowerData.projectilePrefab` field. Firing, impact spawning, colliders, pooling and radius-based damage remain owned by gameplay code.

## Match the explosion to the actual radius

Current `AoETower.effectRadius` defaults to **2 world units**, centered on the primary target's body position. Confirm the actual tower instance's value during hookup. The visual contains no radius/damage behavior and does not change that setting.

The largest measured radial extent over all five frames is **152.5819452 source pixels**, measured from each imported pivot using pixel centers at alpha >=16. At 1024 PPU, the following absolute uniform XY scale makes the farthest visible portion approximately match a chosen radius:

```text
visualScale = effectRadius * 1024 / 152.5819452
```

The supplied explosion prefab uses XY scale **6.711147892892**, representing radius 1. For the current default radius 2, use absolute XY scale **13.422295785784**, or multiply the prefab's existing reference scale by 2. This assumes a unit-scale world-space parent. Account for the actual inherited scale when parented, and avoid nonuniform stretching. Do not additionally multiply by tower art scale: the damage query uses world-space distances.

This is a cosmetic calibration of an irregular burst, not a precise circular boundary: some lobes end inside the radius and very faint pixels below the alpha threshold may lie outside it. The largest frame is broken around an open center to leave enemies visible. Do not use sprite bounds as the damage query, promise an exact radius indicator, or reuse the Skunk's +/-30-degree rule for this tower. Keep the burst centered at the impact location and independent of a despawned projectile or moving target.

## Verification and manual review

Offline checks cover transparency, source/runtime equality, eight padded slices, pivots, normalized radial measurement, GUID/file-ID references, correct frame order, one-shot blank endings and visual-only components. No runtime scripts, tower prefab, gameplay data or scenes were changed. No Unity tests or Unity validation runs were performed, per `AGENTS.md`.

In Unity, preview both clips and compare the shell/flash to the barrel at neutral and both +/-60-degree limits. Confirm the shell emerges ahead of the bore, the flash follows the gun and its thickness clears the operator throughout the launch path. The earlier tower check only covered a thin centerline; it did not prove finite-width shell clearance. After gameplay hookup, inspect the explosion through the final camera at the actual `effectRadius`, check enemies just inside/outside that radius, and verify effect replay/cleanup on pool reuse.
