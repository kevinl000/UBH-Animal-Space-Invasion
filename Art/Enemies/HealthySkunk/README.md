# Healthy Skunk

Tutorial boss artwork, generated with built-in image_gen using the approved Basic Skunk 1 as a style/identity reference. Exact prompt: `prompt-v1.txt`. Original output is preserved unchanged in `healthy-skunk-source-v1.png`; runtime PNG is byte-identical.

The animal has a fuller body and the UFO has a thicker hull. The skunk has a larger striped tail and an empty central bomb outlet between two side thrusters.

## Visual assets

- `Assets/Prefabs/Enemies/HealthySkunkVisual.prefab`: Transform and SpriteRenderer only; use as a child of a gameplay enemy root. The visual Transform scale is 1.5. PPU 529.052092229 normalizes its scale-1 visible width to the basic enemy; default boss visible width is 3.430664 units, exactly 1.5 times the basic enemy at scale 1.
- `Assets/Prefabs/Projectiles/HealthySkunkProjectileVisual.prefab`: reuses the existing SkunkBomb flight sprite at scale 1.3; no duplicate projectile texture. Place under a projectile root, independently of the boss visual so scale is not multiplied twice.
- Sprite import: Point, Clamp, no mipmaps/compression/NPOT resizing, Full Rect mesh, centered pivot, real alpha transparency. Static sprite, no Animator.

## Gameplay handoff

Pilot faces local -X; underside points local -Y. Current enemy gameplay roots face local +Y inward toward the planet, requiring a 180-degree local Z offset on the boss visual child so its underside points inward. The standalone visual stays upright with identity rotation.

Skunk bomb art points +Y, matching the current EnemyProjectile root; visual child uses identity rotation. Reuse existing SkunkBombBurstVisual and SkunkBombHitPuffVisual at their current sizes. A larger projectile does not change damage, collision, or AoE. Skunk puffs belong only on actual damaged targets under the existing angular hit rule.

These prefabs contain no gameplay scripts and cannot be assigned directly to typed EnemyData prefab fields. Runtime spawning, targeting, firing, collisions, HP/rewards, shiny probability/variants and scene wiring are not implemented here. No shared data, scenes or scripts were edited.

## Review

See `../TutorialBosses/preview.html`, `comparison.png`, `assembly.json` and `verification.json` for visual comparison, dimensions and offline checks. No Unity launch or Unity tests were performed. Manually inspect imports and readability through the gameplay camera; after hookup, check inward orientation, outlet/spawn alignment, independent projectile scale and effect replay.
