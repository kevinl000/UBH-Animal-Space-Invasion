# Skunk bomb visuals

The skunk attack uses the existing angular damage rule. The current `Basic Skunk 1.asset` has `aoeSpreadDegrees: 30`: `BaseEnemyData` calls `GetTargetsNear`, and `SimplePlanetDefense` includes living towers within 30 degrees on either side of the impact angle. The former world-radius idea is discarded. This rule can hit different numbers of towers depending on layout; four cardinal slots are 90 degrees apart, so a hit centered on one slot excludes the others.

Visual approach: one small charcoal canister with a mint-green band flies toward the planet; a compact green burst appears at impact; a smaller matching puff can mark each additional tower actually damaged. The separate puffs communicate the hit set without a huge cloud covering the planet. They are not automatically placed by these assets. No damage code, angular spread, enemy data or interfaces were changed.

Built-in image generation used the approved Basic Skunk 1 artwork as its style reference. `prompt-v1.txt` records the exact prompt, `skunk-bomb-sheet-source-v1.png` preserves the original RGBA sheet unchanged, and `sprites.json` records the seven slices, pivots, scales and timing. The runtime sheet is byte-identical to the source.

## Unity assets

Expand `Assets/Art/Projectiles/SkunkBomb/SkunkBombSheet.png` for `SkunkBombFlight`, `SkunkBombBurst01/02/03`, and `SkunkBombHitPuff01/02/03`. The two empty top-row cells are not imported. Each sprite cell is 418 by 418 pixels. Import settings: Point filtering, Clamp wrapping, alpha transparency, no compression/mipmaps, Full Rect mesh, 512 pixels per unit and custom pivots.

| Prefab under `Assets/Prefabs/Projectiles/` | Contents | Default size and timing |
| --- | --- | --- |
| `SkunkBombProjectileVisual.prefab` | Transform and SpriteRenderer | One static canister; visible width about 0.246 units, length 0.361 units. |
| `SkunkBombBurstVisual.prefab` | Transform, SpriteRenderer and Animator | Three frames at 12 fps; blank after 0.25 seconds. Uniform XY scale 2; maximum visible width about 1.355 units. |
| `SkunkBombHitPuffVisual.prefab` | Transform, SpriteRenderer and Animator | Three frames at 12 fps; blank after 0.25 seconds. Scale 1; maximum visible width about 0.266 units. |

The burst and hit puff each have a matching `.anim` and `.controller` in `Assets/Animations/Projectiles/`. Both are non-looping. Gas renderers use alpha 0.75 to keep the towers readable; the bomb uses alpha 1. All use Unity's default sprite material, Default sorting layer and order 10. The largest central burst is roughly two rabbit-tower base widths at the existing base sprite's 100 PPU/scale-1 reference; treat this as a starting visual scale for manual camera review, not a damage radius. Do not enlarge it to bridge excluded towers.

## Gameplay attachment later

The bomb's rounded nose points **local +Y**, matching the inward direction set by `EnemyProjectile.Launch`. Its visual can be a child with identity rotation under that gameplay projectile root. The skunk's outlet lies underneath the UFO; bomb launch positioning at the outlet still needs the gameplay hookup. The prefab is visual-only and cannot be assigned directly to the typed `EnemyData.projectilePrefab` field without a gameplay prefab containing `EnemyProjectile`.

The gas sprites use **bottom-center contact pivots** and grow along local +Y. Position the central burst at the impact point and orient +Y **outward from the planet**, so the cloud rises above the surface. Its orientation is opposite the bomb's inward-facing +Y. Place optional smaller puffs at additional actual damaged towers, oriented outward at each tower's own angle. Use the same hit recipients and angular spread as damage; do not assume there are always two neighbors, skip ahead to the next occupied slot, or place a puff on a tower excluded by the hit query. A center hit on the planet hull can still use the central burst.

All animation keys only swap `SpriteRenderer.m_Sprite`. Gameplay owns movement, target selection, damage, effect spawning, attachment to rotating towers/planet, pool replay and cleanup. No Transform or color animation, animation events, colliders, runtime scripts or additional damage-over-time behavior were added. One-shot prefabs remain alive with an empty renderer at the end; the gameplay hookup must handle their lifetime.

## Manual review

No Unity tests or validation runs were performed, per `AGENTS.md`. Offline checks cover original/runtime equality, transparency, slice bounds, pivots, references, one-shot frame order and visual-only components.

In Unity, inspect the seven imported slices, compare the bomb with the UFO outlet, and preview both clips. Check gas size/alpha through the gameplay camera. After gameplay hookup, compare a 12-slot layout (30-degree spacing), a 16-slot layout (22.5-degree spacing) and a four-cardinal-slot layout (90-degree spacing): show smaller puffs only on towers actually receiving that impact's damage, with none on the four-slot planet's excluded neighbors. Check contact alignment while the planet rotates and replay/cleanup when effects are reused.
