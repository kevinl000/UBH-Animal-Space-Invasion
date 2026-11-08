# Basic Goose 2 artwork

Tier-2 color variant of the approved Basic Goose 1. Current v2 uses a saturated magenta-purple hull and lavender/violet dome for a stronger distinction at small sizes. The Canada goose, orange lights and flame remain visually consistent with tier 1. No extra armor, shield, weapons or animation was added. The user delegated the hue choice, then rejected v1's subtle hull-only shift as insufficiently noticeable.

Generated using built-in `image_gen` in edit mode. Current v2 references the v1 edit; exact prompt and setup: `prompt-v2.txt`. Current original generated pixels are preserved in `basic-goose-2-source-v2.png`; the runtime PNG is byte-identical to v2. The superseded `basic-goose-2-source-v1.png` and `prompt-v1.txt` remain preserved. The edit is visually consistent, not pixel-identical to tier 1.

## Deliverables

- `preview.html`: side-by-side tier comparison with a size control and background choices; defaults to 160 px and requests v2 explicitly to avoid the previous cached runtime image.
- `Assets/Art/Enemies/BasicGoose2/BasicGoose2.png`: 1254 by 1254 transparent sprite.
- `Assets/Prefabs/Enemies/BasicGoose2Visual.prefab`: Transform and SpriteRenderer only, scale 1, centered pivot, default sprite material and sorting order 0. No gameplay components or Animator.
- `assembly.json`: dimensions, import scale, facing conventions and existing projectile references.
- `verification.json`: offline checks and remaining review limits.

The Point/Clamp importer uses no mipmaps, compression or NPOT resizing and a Full Rect mesh. PPU is 515.518900343643, compensating for the generated image's approximately 0.7% wider visible silhouette. At alpha >= 128 both tiers have a visible width of 2.2734375 world units. Extremely faint edge pixels are retained unchanged and are excluded from this visible-width measurement. Existing sprite, folder and prefab GUIDs are preserved.

## Gameplay handoff

The requested/README row has 2 HP, 1 money, 1 fuel and 1 single-target damage. The current Basic Goose 2 data already specifies those values. Higher-level spawning and balance remain gameplay work; this task changes no data, scenes or runtime scripts.

Reuse `Assets/Prefabs/Projectiles/GoosePoopProjectileVisual.prefab` and `GoosePoopImpactVisual.prefab` at their existing scale. No duplicate projectile texture or tier-specific projectile is needed.

Pilot faces local -X; underside points local -Y. The current enemy gameplay root points +Y inward, so a future visual child needs local Z rotation 180 degrees to point its underside inward. The standalone visual prefab remains upright with identity rotation. Goose poop art points +X; its visual child needs +90 degrees under the current +Y-forward EnemyProjectile root. Future gameplay owns launch position, movement, firing, collision, HP and pooling. This visual-only prefab cannot be assigned directly to EnemyData's typed gameplay-prefab field.

## Verification and manual review

Offline checks passed: source/runtime byte equality, real alpha, matched visible width, unique new GUIDs, prefab sprite reference, visual-only components and import settings. No Unity launch or Unity tests were performed.

In Unity, inspect the import and drag the visual prefab beside Basic Goose 1. Confirm the hue difference is readable at the intended camera size, with correct material/transparency and sorting. After gameplay hookup, check inward orientation and poop spawn alignment. Original Basic Goose 1, other artwork and shared components were left untouched.
