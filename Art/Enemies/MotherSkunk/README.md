# Mother Skunk — Level 2 boss

Approved heavy bomber design: a broad black-and-ivory skunk with a large curled striped tail and focused expression, under a low blue dome. The wide, shallow UFO uses deep plum panels, charcoal structure, lavender edges, six mint lights and two cyan/mint side thrusters. A single broad, empty central bomb bay leaves the downward drop path clear. No projectile, gas or fixed volley count is drawn into the sprite.

Generated with built-in `image_gen` using Healthy Skunk and Basic Skunk 3 as style/identity references. `prompt-v1.txt` records the exact prompt. `mother-skunk-source-v1.png` preserves the original output; `Assets/Art/Enemies/MotherSkunk/MotherSkunk.png` is byte-identical. Earlier artwork remains unchanged.

## Visual prefab

`Assets/Prefabs/Enemies/MotherSkunkVisual.prefab` contains a static SpriteRenderer and one empty child, `BombOutlet`. No gameplay component, Animator, collider or automatic firing is included.

The visual Transform scale is 1.5. PPU is 536.047822374039, normalizing scale-1 visible width to 2.287109375 units. Default visible width is 3.4306640625 units: equal to Healthy Skunk and 1.5 times Basic Skunk 3. The flatter silhouette distinguishes this bomber from the deeper tutorial-boss hull. Width measurements use alpha >=128.

The 1254 by 1254 source has real alpha transparency. Import settings: Sprite/Single, Point, Clamp, Full Rect, centered pivot, no compression/mipmaps/NPOT resizing. Very faint generated alpha below 16/255 reaches canvas edges; the visible silhouette has transparent padding and is not clipped. Source alpha is preserved unchanged.

`BombOutlet` is at source pixel (627,1042), measured from top-left, just below the central bay lip. Its local position is (0,-0.774184657932,0). It inherits the visual's scale/rotation. This is a handoff marker, not an implemented launch position: the current EnemyProjectile.Launch sets its own polar start position. Future gameplay must align the spawn and offset the bomb center inward enough for the back of the bomb to clear the bay lip.

## Reused bomb and gas visuals

Reuse `Assets/Prefabs/Projectiles/HealthySkunkProjectileVisual.prefab`, which already uses the existing SkunkBombFlight sprite at scale 1.3. No new projectile texture or duplicate projectile prefab is needed. Keep the projectile independent of the 1.5-scale enemy visual so the two scale factors are not multiplied.

Reuse `SkunkBombBurstVisual.prefab` and `SkunkBombHitPuffVisual.prefab` at their current sizes. Each bomb's gas is placed from the real impact and actual damage recipients. The existing angular AoE is 30 degrees on either side of impact; larger bombs do not enlarge this area, guarantee adjacent hits or add damage over time. Effect playback and cleanup remain gameplay work. See `../../Projectiles/SkunkBomb/README.md` for the current effect handoff.

## Gameplay handoff

The row specifies 35 HP, orbiting speed 1, money/fuel drops 15 each, and n bombs with 3 AoE damage per bomb. The current MotherSkunkBoss defaults to three bombs with 25-degree spacing; the data currently has a four-second attack interval. These are existing, unconfirmed tuning values and were not edited or encoded in the visual asset.

Pilot faces local -X; outlet points local -Y. Current enemy roots point local +Y inward toward the planet, requiring a 180-degree local Z offset on the boss visual child. The standalone prefab stays upright with identity rotation. Bomb art points +Y and uses identity rotation under the current EnemyProjectile root. Gas grows +Y outward from the surface, opposite the bomb's inward direction.

This visual-only boss cannot be assigned directly to EnemyData's typed gameplay-prefab field. Stats, volley timing/count/spread, damage, spawning, collisions, scenes and shiny variants were not changed.

## Review

Open `preview.html` directly in a browser for comparison with Healthy Skunk and Basic Skunk 3 at the same world scale, with camera-size/background controls and optional outlet marker/bomb-size sample. The sample shows one reused bomb's size and facing only, not volley count or target selection.

`comparison.png` is the static size review. `assembly.json` records dimensions, pivots, marker position and existing effect references; `verification.json` records offline checks. No Unity launch or Unity tests were performed. Manually inspect imports/materials and camera readability, inward orientation, launch alignment and bomb clearance. After gameplay hookup, confirm gas puffs match actual damaged targets and pooled effects replay/clean up correctly.
