# Restraining Buck frozen-status frame

One sparse pale-cyan/ivory U-shaped frost frame, with a low icy base, broken side clusters and short icicles. Its center and top are open and transparent so the rabbit, controls and weapon remain readable. No tower is baked into this sprite.

Generated with built-in `image_gen` using Restraining Buck for palette/style and the existing LaserPortrait only as a target-readability reference. Exact prompt: `prompt-v1.txt`. `buck-frozen-status-source-v1.png` preserves the original RGBA output; `Assets/Art/Effects/BuckFreeze/BuckFrozenStatus.png` is byte-identical. Existing tower art was not edited.

`Assets/Prefabs/Effects/BuckFrozenStatusVisual.prefab` contains Transform and SpriteRenderer only. No Animator, timer, collider or gameplay script. Sprite import: Single, Point, Clamp, Full Rect, alpha transparency, no compression/mipmaps/NPOT resizing. PPU is 941.818181818182; visible width is 1.1 units at scale 1. Renderer alpha is 0.8. `assembly.json` records the measured bottom-center contact pivot; local +Y grows upward from the base.

Position at the affected tower's ground/base contact, oriented with +Y outward from the planet. Fit to the tower's actual visible footprint and account for inherited scale once; 1.1 units is a starting art size, not a targeting/damage area. Keep the opening clear around the rabbit and weapon.

Future gameplay must show the frame only on the actual frozen recipient and remove it on thaw, death or despawn. The asset has no fixed freeze duration and does not disable a tower. Current RestrainingBuckBoss applies freeze at firing time, independently of projectile impact; this art pass leaves that behavior and its unconfirmed duration unchanged. No ice AoE, additional target or damage-over-time behavior is implied.

Preview and offline checks: `../../Enemies/RestrainingBuck/preview.html` and `verification.json`. No Unity launch or Unity tests were performed. Manually check target fit, sorting/materials and camera readability, then status lifetime/removal after gameplay hookup.
