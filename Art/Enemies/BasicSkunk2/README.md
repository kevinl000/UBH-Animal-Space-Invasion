# Basic Skunk 2 artwork

Tier-2 hue variant of the approved Basic Skunk 1: copper-orange hull and amber glass dome. The black-and-white pilot, upright striped tail, six mint lights, two small cyan/mint side thrusters and empty gray-rimmed central bomb outlet remain visually consistent. The warm palette distinguishes this tier from Skunk 1's charcoal/navy hull and blue dome at small sizes. Higher HP is communicated through color alone; no armor, shield or extra weapons were added.

Generated with built-in `image_gen` in edit mode using `Assets/Art/Enemies/BasicSkunk1/BasicSkunk1.png` as the edit target. Exact prompt and setup: `prompt-v1.txt`. Original generated RGBA pixels are preserved in `basic-skunk-2-source-v1.png`; the runtime PNG is byte-identical. Geometry is visually consistent, not pixel-identical to the reference.

## Deliverables

- `preview.html`: side-by-side Skunk 1/2 comparison, default 160 px, adjustable 80–400 px, with space/light/transparency-grid backgrounds.
- `Assets/Art/Enemies/BasicSkunk2/BasicSkunk2.png`: transparent 1254 by 1254 runtime sprite with new metadata.
- `Assets/Prefabs/Enemies/BasicSkunk2Visual.prefab`: Transform and SpriteRenderer only, scale 1, centered pivot, identity rotation, default sprite material and sorting order 0.
- `assembly.json`: dimensions, import scale, facing and existing bomb-effect references.
- `verification.json`: offline checks and review limits.

Import settings: Point, Clamp, no mipmaps/compression/NPOT resizing, Full Rect mesh. PPU is 513.311699402220, normalizing the generated silhouette to Skunk 1's visible width of 2.287109375 world units at scale 1. Visible-width measurements use alpha >= 128; faint edge pixels remain preserved unchanged. The bomb-drop path is visually clear between the two side thrusters.

## Gameplay handoff

The requested/README row and current Basic Skunk 2 data specify 4 HP, 2 money, 2 fuel and 2 AoE damage. No gameplay data, progression, scenes or runtime scripts were changed. This visual-only prefab cannot be assigned directly to EnemyData's typed gameplay-prefab field.

Reuse the existing `Assets/Prefabs/Projectiles/SkunkBombProjectileVisual.prefab`, `SkunkBombBurstVisual.prefab` and `SkunkBombHitPuffVisual.prefab` at their existing scales. No tier-specific projectile or gas texture is needed.

Pilot faces local -X; bomb outlet points local -Y. Current enemy gameplay roots face +Y inward toward the planet, so a future visual child needs local Z rotation 180 degrees to aim the outlet inward. The standalone visual remains upright. Existing bomb art faces +Y and needs no visual-child rotation under the current +Y-forward EnemyProjectile root. Gas grows +Y outward from the surface, opposite the inward bomb direction. Outlet spawn alignment, firing, movement, collisions and pooling remain gameplay work.

The existing AoE rule uses 30 degrees on either side of impact, not a world-space radius or a guaranteed number of adjacent towers. Reuse the central burst and put smaller puffs only on additional actual damage recipients. This hue variant does not enlarge effects, change spread or add damage over time. See `Art/Projectiles/SkunkBomb/README.md` for the established effect handoff.

## Verification and manual review

Ten offline checks passed: source/runtime byte equality, real alpha, matched visible width, unique GUIDs, prefab sprite reference, visual-only components, import settings, texture-size limits, preview image references and existing bomb-effect references. No Unity launch or Unity tests were performed.

In Unity, compare both tiers through the intended gameplay camera. Confirm the orange distinction, pilot/tail readability, transparency/material and sorting. After gameplay hookup, check that the outlet faces inward and the bomb spawns without overlapping either thruster; review gas placement only on actual damage recipients. Original Skunk 1 and unrelated artwork remain unchanged.
