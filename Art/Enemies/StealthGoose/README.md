# Stealth Goose artwork

Approved direction: the familiar Canada goose UFO with a charcoal/slate hull, smoky violet dome and dim cyan lights. It remains faintly visible to the player before Radar detection, then becomes fully visible permanently. The existing mint marker flashes briefly on newly revealed enemies. There is no flight-frame animation or new projectile/effect artwork.

Generated using built-in `image_gen` in edit mode with Basic Goose 1 as the inspected edit target. `prompt-v1.txt` preserves the exact prompt and mode. `stealth-goose-source-v1.png` is the unchanged original RGBA output, and `Assets/Art/Enemies/StealthGoose/StealthGoose.png` is byte-identical. The silhouette is visually consistent with the family; generated pixels are not identical to the reference. Source transparency is preserved, with the ship itself fully visible. Stealth opacity is a separate renderer setting.

## Deliverables

- `preview.html`: local side-by-side size comparison, faint-opacity and background controls, and an interactive Radar reveal demonstration. Repeated scans leave the ship visible and flash no new marker. Reset explicitly returns the demonstration to stealth.
- `Assets/Art/Enemies/StealthGoose/StealthGoose.png`: 1254 x 1254 transparent single sprite.
- `Assets/Prefabs/Enemies/StealthGooseVisual.prefab`: Transform and SpriteRenderer only, centered pivot, identity rotation/scale, white renderer tint with alpha 1, default sprite material and sorting order 0. No scripts, colliders, Animator or state binding.
- `assembly.json`: source/import settings, facing axes, opacity suggestions and existing marker/projectile references.
- `verification.json`: inexpensive offline checks and remaining manual limits.

Point/Clamp imports disable mipmaps, compression and NPOT resizing and use a Full Rect mesh. PPU is **512.439862542955**, matching Basic Goose 1's **2.2734375-unit visible width** at scale 1. Bounds use alpha >=128, excluding faint generated edge pixels without changing them. Generated ship pixels are predominantly alpha 253/254 (nearly opaque); the original alpha is retained rather than thresholded or repainted. This is the existing art-family scale; final gameplay size needs camera review.

## Stealth and Radar handoff

Use renderer alpha **0.18** as an initial camera-review value before detection and **1** after detection. The approved behavior is faint visibility, not a committed exact opacity. Only tint alpha changes; source pixels remain unchanged. The standalone visual prefab intentionally shows the fully visible sprite so it can be reviewed without runtime state. Future gameplay must apply the initial stealth alpha from the actual state, make it fully visible when revealed, and reset alpha/state on pooled reuse. Do not add an automatic loop or timer that makes this ordinary enemy become stealthy again.

Reuse `Assets/Prefabs/Effects/RadarRevealMarkerVisual.prefab` for the existing 0.24-second one-shot marker. The shared marker prefab is unchanged. Its scale 1.75 was sized for a small demonstration; for this visual at scale 1, use an initial **uniform instance scale 7.7867177996832755** in visual-local space, positioned at **(-0.000975724248927, -0.0497619366953, 0)** relative to the centered visual root. This fits all three existing marker frames around the whole ship, including the engine, with at least 8% padding at the recorded alpha thresholds. If attaching under a differently scaled or rotated root, transform the position/scale accordingly. The browser uses that same fit. Review the final camera and sorting; play the marker above the enemy, once when its state changes. The shared marker's order 10 is only an initial sorting choice.

Radar's existing fixed dish and 90-degree scan pulse are reused in the browser. The tower has a separate diagram scale there; the preview does not establish gameplay relative size, range or interval. Already revealed enemies receive no new detection marker. The ship stays visible after the marker ends. No shared Radar source, prefab, clip or preview was edited.

## Gameplay handoff

README and current `Stealth Goose.asset` already specify 1 HP, 1 money, 1 fuel, 1 single-target damage and `isStealth: 1`. The gameplay enemy/projectile prefab fields are currently empty. This visual-only prefab cannot be assigned to a field typed as a gameplay enemy component.

Pilot faces local **-X**, and the underside points **-Y**. Current enemy roots point local **+Y** inward toward the planet, so place this upright artwork under the gameplay root with a future visual-child Z offset of **180 degrees**. The marker attachment coordinates above are in that visual child's space. Reuse `Assets/Prefabs/Projectiles/GoosePoopProjectileVisual.prefab` and `GoosePoopImpactVisual.prefab`; the +X-forward poop artwork needs a +90-degree visual-child offset under the current +Y-forward `EnemyProjectile` root. Existing projectile scale remains unchanged.

`BaseEnemyData.RevealStealth()` permanently clears targetability stealth, but does not change SpriteRenderer alpha. `RadarTower.Fire()` currently checks distance using `data.fov` without an angular test, and `BaseTowerData.Tick()` skips firing for attack type `None`. Runtime visual binding, Radar scheduling/range/sector filtering, firing/spawn alignment, movement, collision and pooling remain gameplay implementation work. Scripts, data, scenes, managers, gameplay prefabs and shared effects were not changed.

## Verification and manual review

Offline checks cover real alpha, source/runtime byte equality, matched visible width, import settings, metadata pairs, unique new GUIDs, prefab references/components, all marker-frame bounds and preview file references. Browser JavaScript syntax and browser interaction checks are recorded in `verification.json`. No Unity launch or Unity tests were performed.

In Unity 6000.6.7f1, review the new import and visual prefab beside Basic Goose 1 through the intended camera. Confirm silhouette/palette readability, material/transparency, scale and sorting. After gameplay hookup, check faint-to-full visibility, permanent reveal after the marker clears, marker fit, correct inward orientation, poop spawn alignment and a fresh stealth appearance after pooled reuse.
