# Basic Goose 3 artwork

Tier-3 color variant: emerald-green hull and green glass dome with mint highlights. The natural Canada goose, orange/gold lamps, trim and thruster remain visually consistent with the approved family. Blue tier 1, magenta-purple tier 2 and green tier 3 use the same silhouette and visible width. Color alone distinguishes the tiers; no armor, shields, extra weapons or animation were added.

Generated with built-in `image_gen` in edit mode using the approved `Assets/Art/Enemies/BasicGoose2/BasicGoose2.png` (v2) as the edit target. Exact prompt and setup: `prompt-v1.txt`. Original generated RGBA pixels are preserved unchanged in `basic-goose-3-source-v1.png`; the runtime copy is byte-identical. Geometry is visually consistent, not pixel-identical to the reference.

## Deliverables

- `preview.html`: all three tiers side by side, default 160 px, adjustable 80–400 px, with space/light/transparency-grid backgrounds. The earlier tier-2 preview remains unchanged.
- `Assets/Art/Enemies/BasicGoose3/BasicGoose3.png`: transparent 1254 by 1254 runtime sprite with unique metadata.
- `Assets/Prefabs/Enemies/BasicGoose3Visual.prefab`: Transform and SpriteRenderer only; scale 1, centered pivot, identity rotation, default sprite material and sorting order 0.
- `assembly.json`: dimensions, import scale, facing and projectile references.
- `verification.json`: offline checks and remaining review limits.

Import settings: Point, Clamp, no mipmaps/compression/NPOT resizing, Full Rect mesh. PPU is 516.838487972509, normalizing the generated silhouette to a visible width of 2.2734375 world units, equal to Basic Goose 1 and 2 at scale 1. Width measurements use alpha >= 128; faint edge pixels remain preserved unchanged in the PNG.

## Gameplay handoff

The requested/README row and current Basic Goose 3 data specify 4 HP, 1 money, 1 fuel and 1 single-target damage. No gameplay data, spawn progression, scenes or scripts were changed. This is a visual-only asset, not a working gameplay enemy prefab, and cannot be assigned directly to EnemyData's typed gameplay-prefab field.

Reuse the existing `Assets/Prefabs/Projectiles/GoosePoopProjectileVisual.prefab` and `GoosePoopImpactVisual.prefab` at their existing scale; no tier-specific projectile texture is needed.

Pilot faces local -X; underside points local -Y. Current enemy roots face +Y inward toward the planet, so a future visual child needs local Z rotation 180 degrees to point the underside inward. The standalone visual remains upright. Poop art faces +X and needs a +90-degree visual-child offset under the current +Y-forward EnemyProjectile root. Future gameplay owns movement, launch point, firing, collisions and pooling.

## Verification and manual review

Ten offline checks passed: source/runtime byte equality, real alpha, matched visible width, unique GUIDs, sprite reference, visual-only components, import settings, texture-size limits, three preview image references and existing projectile/impact references. No Unity launch or Unity tests were performed.

In Unity, review all three imports side by side at the intended gameplay-camera size. Confirm the green tier is readable, materials/transparency and sorting are correct, and size matches. After gameplay hookup, check inward orientation and projectile spawn alignment. Approved earlier artwork remains unchanged.
