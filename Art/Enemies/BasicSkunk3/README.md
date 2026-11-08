# Basic Skunk 3 artwork

Tier-3 hue variant: crimson hull and ruby/rose-red glass dome. The natural black-and-white pilot, striped tail, mint running lights, cyan/mint side thrusters and empty gray-rimmed bomb outlet remain visually consistent with the earlier tiers. Navy/blue tier 1, copper/amber tier 2 and crimson/red tier 3 share the same silhouette and visible width. Higher HP is represented by color alone; no armor, shield or new weapons were added.

Generated with built-in `image_gen` in edit mode, using `Assets/Art/Enemies/BasicSkunk2/BasicSkunk2.png` as the edit target. Exact prompt and setup: `prompt-v1.txt`. Original generated RGBA pixels are preserved unchanged in `basic-skunk-3-source-v1.png`; the runtime PNG is byte-identical. Geometry is visually consistent, not pixel-identical to the reference.

## Deliverables

- `preview.html`: all three tiers side by side, default 160 px, adjustable 80–400 px, with space/light/transparency-grid backgrounds. Previous previews remain unchanged.
- `Assets/Art/Enemies/BasicSkunk3/BasicSkunk3.png`: transparent 1254 by 1254 runtime sprite with new metadata.
- `Assets/Prefabs/Enemies/BasicSkunk3Visual.prefab`: Transform and SpriteRenderer only, scale 1, centered pivot, identity rotation, default sprite material and sorting order 0.
- `assembly.json`: dimensions, import scale, facing and existing bomb-effect references.
- `verification.json`: offline checks and remaining review limits.

Import settings: Point, Clamp, no mipmaps/compression/NPOT resizing, Full Rect mesh. PPU is 512.437233134074, normalizing the generated silhouette to the earlier skunks' visible width of 2.287109375 world units at scale 1. Measurements use alpha >= 128; faint edge pixels are retained unchanged. The empty downward bomb-drop path remains visually clear between the two side thrusters.

## Gameplay handoff

The requested/README row and current Basic Skunk 3 data specify 8 HP, 2 money, 2 fuel and 2 AoE damage. No gameplay data, progression, scenes or runtime scripts were changed. This visual-only prefab cannot be assigned directly to EnemyData's typed gameplay-prefab field.

Reuse existing `Assets/Prefabs/Projectiles/SkunkBombProjectileVisual.prefab`, `SkunkBombBurstVisual.prefab` and `SkunkBombHitPuffVisual.prefab` at their existing scales; no tier-specific textures or effects are needed.

Pilot faces local -X; bomb outlet points local -Y. Current enemy gameplay roots point +Y inward toward the planet, so a future visual child needs local Z rotation 180 degrees. The standalone visual stays upright. Existing bomb art faces +Y and needs no visual-child rotation under the current +Y-forward EnemyProjectile root. Gas grows +Y outward from the surface, opposite the bomb's inward direction. Future gameplay owns outlet spawn alignment, movement, firing, collisions and pooling.

Existing AoE uses 30 degrees on each side of impact. Show small gas puffs only on additional actual damage recipients; the art does not change spread, guarantee adjacent tower hits or add damage over time. See `Art/Projectiles/SkunkBomb/README.md` for effect placement and replay details.

## Verification and manual review

Ten offline checks passed: source/runtime byte equality, real alpha, matched visible width, unique GUIDs, prefab sprite reference, visual-only components, import settings, texture-size limits, three preview image references and existing bomb-effect references. No Unity launch or Unity tests were performed.

In Unity, review all three tiers at the intended gameplay-camera size. Confirm the red tier is distinct, pilot/tail and outlet remain readable, and transparency/material, sorting and scale are correct. After gameplay hookup, check inward orientation, bomb outlet alignment and gas placement only on actual damage recipients. Earlier approved artwork remains unchanged.
