# Mother Goose broadcast call

Three expanding hollow teal/cyan radio-ring sprites, generated with built-in `image_gen` using the Mother Goose sprite as the palette/style reference. Exact prompt: `prompt-v1.txt`. Original RGBA output: `mother-goose-call-sheet-source-v1.png`. Runtime sheet: `Assets/Art/Effects/MotherGooseCall/MotherGooseCallSheet.png`, byte-identical to the original. No pixels were repainted.

The square sheet uses four equal 627 by 627 cells: frame 1 top-left, frame 2 top-right, frame 3 bottom-left. The bottom-right cell is visibly empty and is not imported. `sprites.json` records the three Unity rectangles, file IDs and per-frame pivots. Custom pivots align each ring's measured center to the antenna without shifting source pixels.

Import settings: Sprite/Multiple, Point, Clamp, Full Rect, no compression/mipmaps/NPOT resizing, alpha transparency, 497.222222222222 PPU. The largest frame is 0.72 units wide at scale 1, or 1.08 units when parented at scale 1 under the 1.5-scale Mother Goose visual.

## Playback and attachment

- Visual prefab: `Assets/Prefabs/Effects/MotherGooseCallVisual.prefab` (Transform, SpriteRenderer and Animator).
- Clip/controller: `Assets/Animations/Effects/MotherGooseCall/MotherGooseCall.anim` and `.controller`.
- Three sprite keys at 0, 0.125 and 0.25 seconds; final empty sprite at 0.375 seconds. 8 fps, no looping, no animation events or root motion. The clip only changes SpriteRenderer.m_Sprite, leaving position, rotation and scale to gameplay.
- Attach a future effect instance under MotherGooseVisual/CallOrigin at local position zero, identity rotation and scale 1. It is supplied separately and is not preattached or automatically triggered by the boss.

The default Animator state `Call` plays once. Future gameplay owns spawning, replay (for example restarting `Call` at normalized time zero), cleanup/pool return and synchronization with a successful flock call. The final blank frame does not destroy or despawn the effect. No damage, AoE, healing or reveal behavior is implied.

Preview in `../../Enemies/MotherGoose/preview.html`; verification is recorded in that folder's `verification.json`. No Unity launch or Unity tests were performed. Manually check the imported slices, antenna attachment, frame-center stability, brief final disappearance and repeat playback after gameplay hookup.
