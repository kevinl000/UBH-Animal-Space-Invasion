# Mother Goose — Level 1 boss

Approved design: a natural Canada goose with upright posture, fuller body, raised wing and open beak, piloting a broad cream-and-teal command UFO with a blue dome, cyan running lights and a small antenna on the right rim. No gun, bomb outlet or projectile is included. The broadcast animation is a separate asset.

Generated with built-in `image_gen` using Healthy Goose and Basic Goose 3 as style/identity references. `prompt-v1.txt` preserves the exact prompt. `mother-goose-source-v1.png` preserves the original generated RGBA output; `Assets/Art/Enemies/MotherGoose/MotherGoose.png` is byte-identical. Approved earlier artwork remains unchanged.

## Visual prefab and scale

`Assets/Prefabs/Enemies/MotherGooseVisual.prefab` contains a Transform and SpriteRenderer plus one empty child, `CallOrigin`, at the antenna tip. No gameplay component or Animator is attached to the boss.

The boss visual Transform scale is 1.5. PPU is 531.793814432990, normalizing the source visible width to 2.2734375 units at scale 1. Default visible width is 3.41015625 units, equal to Healthy Goose and 1.5 times Basic Goose 3. Width is measured at alpha >=128, excluding transparent padding.

The source is 1254 by 1254 pixels. Import settings: Sprite/Single, Point, Clamp, Full Rect mesh, centered pivot, no compression/mipmaps/NPOT resizing, real alpha transparency. Faint generated alpha below 16/255 reaches a canvas edge; the visible silhouette has padding and is not clipped. Source alpha is preserved unchanged.

## Broadcast attachment

`CallOrigin` corresponds to source pixel (1076, 447), measured from the top-left. Its local position is approximately (0.844312190, 0.338477047, 0). The marker inherits boss scale and rotation.

The separate `Assets/Prefabs/Effects/MotherGooseCallVisual.prefab` plays three expanding radio-ring frames at 8 fps, then becomes blank at 0.375 seconds. For a future gameplay hookup, instantiate or reuse it under `CallOrigin` with local position zero, identity rotation and scale 1. Its largest visible width is then 1.08 world units at boss scale 1.5. If positioned independently at the marker's world position, apply the equivalent inherited scale explicitly. Do not apply boss scale twice.

The effect is not attached to the boss prefab, so the static boss does not broadcast automatically. Future gameplay should play it once when a flock is actually called in. The existing MotherGooseBoss has no effect trigger hookup; replay, pooling and cleanup remain gameplay work. See `../../Effects/MotherGooseCall/README.md` and `sprites.json` for animation details. These rings communicate a call, not an AoE or damage radius.

## Gameplay handoff

The row specifies 40 HP, orbiting speed 1, money/fuel rewards 15 each, zero damage and periodic calls for Basic Goose 3. These assets do not change that data or behavior. Existing flock size, interval and spread are unconfirmed code defaults; no balance values were inferred or edited.

Pilot faces local -X; underside points local -Y. Current gameplay enemy roots face local +Y inward toward the planet, so using this prefab as a visual child requires local Z rotation 180 degrees to point the underside inward. The standalone visual remains upright with identity rotation. The circular signal needs no separate aiming convention.

This is a visual prefab and cannot be assigned directly to EnemyData's field typed as a gameplay enemy. Runtime spawning, flock references, movement, collisions, HP/rewards, shiny variants and scene wiring are outside this art pass. Mother Skunk was not created.

## Review

Open `preview.html` directly in a browser. It compares Mother Goose, Healthy Goose and Basic Goose 3 at the same world scale, with replay/background/camera-size controls and an antenna marker toggle. Repeating the call in the preview is for visual review only. It does not simulate flock size or spawn timing.

`comparison.png` is a static size review; `assembly.json` records geometry and references; `verification.json` records offline checks. No Unity launch or Unity tests were performed. Manually inspect Unity imports/materials and camera readability, the inward visual orientation, antenna alignment, and call replay/cleanup after gameplay hookup.
