# Restraining Buck — Level 3 boss

Approved design: broad-chested brown buck with ivory muzzle/chest and branching ivory/tan antlers fully inside a taller blue dome. The steel-gray UFO uses navy structure, pale cyan lights, a single compact downward launcher and two side thrusters. The buck retains its antlers; projectile art is separate.

Generated with built-in `image_gen` using Healthy Goose and Mother Skunk as family/style references. `prompt-v1.txt` preserves the exact prompt. `restraining-buck-source-v1.png` preserves the original RGBA output; `Assets/Art/Enemies/RestrainingBuck/RestrainingBuck.png` is byte-identical. No earlier artwork was replaced.

## Visual prefab and size

`Assets/Prefabs/Enemies/RestrainingBuckVisual.prefab` contains a static SpriteRenderer and empty `Muzzle` child. No gameplay component, collider or Animator is attached to the boss.

The visual Transform scale is 1.5. PPU 539.711340206186 normalizes scale-1 visible width to the Basic Goose 1 reference (2.2734375 units). Default width is 3.41015625 units, matching Healthy Goose. No Basic Deer artwork exists as a size reference. Bounds use alpha >=128.

The source canvas is 1254 by 1254. Import settings: Sprite/Single, Point, Clamp, Full Rect mesh, centered pivot, no mipmaps/compression/NPOT resizing. Real alpha is preserved; very faint generated pixels below alpha 16/255 touch an edge, but the visible silhouette has padding and is not clipped.

`Muzzle` is at source pixel (627,1227), measured from top-left, just beyond the launcher lip. Local position is approximately (0,-1.111705378973,0). Its local Z rotation is 180 degrees, making marker +Y point down/out of the launcher in the upright sprite. It inherits the boss's scale/rotation. Current gameplay uses polar projectile start positions and does not consume this marker; future hookup needs launch alignment and a forward offset so the back of the 0.48-unit horn clears the muzzle.

## Projectile and frozen status

- `Assets/Prefabs/Projectiles/BuckHornProjectileVisual.prefab`: static ivory antler shard with cyan frost, 0.48 units long, local +Y forward. No projectile gameplay component.
- `Assets/Prefabs/Projectiles/BuckHornImpactVisual.prefab`: compact three-frame frost/ice-chip impact, 12 fps, blank after 0.25 seconds, non-looping. No damage, timer or despawn behavior.
- `Assets/Prefabs/Effects/BuckFrozenStatusVisual.prefab`: static open U-shaped frost frame, visible width 1.1 units at scale 1 and renderer alpha 0.8. Bottom-center contact pivot, local +Y rises from the tower's base. Fit it to each target's actual footprint, accounting for parent scale once. Keep the center/top open so the rabbit and weapon remain readable.

Source sheets and exact prompts are under `Art/Projectiles/BuckHorn/` and `Art/Effects/BuckFreeze/`. Their handoff notes describe slices, pivots, playback and lifetime ownership. The projectile is independent of the 1.5-scale boss visual; do not multiply the scales by parenting it under the visual.

## Gameplay handoff and timing

The row specifies 50 HP, orbiting speed 1, money/fuel drops 20 each, 5 single-target damage, and freezing the nearest tower for n seconds. No data, stats, scripts, scenes or tuning were changed.

The current RestrainingBuckBoss calls FireAt and immediately calls FreezeNearestTower; with a moving projectile, freeze therefore starts at firing time. Its three-second freeze and the data's four-second attack interval are existing unconfirmed defaults. This art pass does not synchronize the freeze with impact or change those values.

The frost impact belongs at the damage impact point; the status frame belongs on the actual frozen tower for its real frozen lifetime, which can differ from the damaged slot and the projectile's travel time. The preview shows independent samples and imposes no timing decision. Runtime targeting, recipient reporting, effect triggering/replay, thaw/removal and pooling remain gameplay work. Neither the status sprite nor impact clip implements freeze.

Pilot faces local -X; launcher points local -Y. Current gameplay enemy roots face local +Y inward, so a future boss visual child needs local Z rotation 180 degrees. A +Y-forward horn visual uses identity rotation under the current EnemyProjectile root. Boss marker +Y already follows the launcher direction. No gameplay root or manager was edited, and visual-only prefabs cannot be assigned directly to fields typed as gameplay enemy/projectile components.

## Review

Open `preview.html` directly in a browser. The upper panel compares boss sizes; the lower samples enlarge the horn, impact and frozen tower 3x for inspection. Freeze status is controlled by a checkbox, not a simulated game duration. The existing LaserPortrait is used only as an unchanged tower reference.

`assembly.json` records asset paths, dimensions, facing and marker geometry. `comparison.png` is a static size review; `verification.json` records offline checks. No Unity launch or Unity tests were performed. Manually review imports/materials, gameplay-camera readability, antler/dome clearance, inward orientation, muzzle offset and projectile fit. After gameplay hookup, check the actual frozen recipient, status lifetime/removal, and pooled impact replay/cleanup.
