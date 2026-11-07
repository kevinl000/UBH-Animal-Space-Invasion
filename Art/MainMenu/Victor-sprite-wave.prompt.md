# Victor sprite wave

Generated using the built-in image generation tool on 2026-11-02. Both outputs have real alpha transparency. Import rectangles, placement, and editing instructions are in `README.md`.

The background uses the existing clean `Art/MainMenu/MainPage-background.png`; no new scenery was generated. The UFO artwork and hover script are unchanged.

## Stationary body

Input/edit target: `Assets/Art/MainMenu/MainPage-background.png` as it existed before this update (the same artwork retained in `Art/MainMenu/MainPage-background-wave.png`).

Output: `Assets/Art/MainMenu/MainPage-victor-body.png` (1672 x 941).

Exact prompt:

> Use case: background-extraction. Asset type: existing Unity pixel-art menu character cutout. Edit target: the supplied 1672 x 941 menu image. Extract ONLY Victor E. Bull's stationary body at the lower right: blue bull with white horns/muzzle, blue shirt with white bull emblem, shorts, sneakers, his lowered LEFT arm (viewer's right), and tail. His raised RIGHT arm (viewer's left) is already absent in this source: KEEP IT ABSENT because an animated arm will be attached later. Preserve every visible character pixel, exact shape, face, stance, clothing, colors, chunky pixel edges, scale and original position. Do not redesign, redraw, add a raised arm, or add scenery. Preserve the SAME full 1672 x 941 landscape canvas and original lower-right placement; do not enlarge or center the character. Everything outside the stationary character must be genuinely transparent, including between his legs, around his horns, and around the tail. No ground shadow, ground, grass, trees, monument, stars, text, labels, checkerboard or opaque background. Output PNG with real alpha.

The generated character occupied a larger source rectangle than requested. Unity's sprite crop and display size restore its menu placement; the source is preserved as generated.

## Raised-arm frames

Input/edit target: `Assets/Art/MainMenu/MainPage-victor-arm.png`.

Output: `Assets/Art/MainMenu/MainPage-victor-wave.png` (1254 x 1254).

Exact prompt:

> Use case: precise-object-edit. Asset type: 2D pixel-art ARM-ONLY animation sprite sheet for a Unity menu. Input: the transparent image contains Victor E. Bull's blue raised RIGHT arm near the lower-right corner. Preserve that exact blue arm design, hand shape, shading, dark outline and blocky pixel art; no full character. Make FOUR frames of a restrained friendly wave as a 2 by 2 grid on a 1024 x 1024 transparent PNG. Each cell is exactly 512 x 512, with no gutters, borders or labels. In EVERY cell place the SHOULDER ATTACHMENT at cell-local pixel (416, 320) from the top-left, with the arm extending leftwards and up from that point. The shoulder and upper arm at the right stay stationary and identical; only the elbow/forearm/hand flex slightly, with the hand moving gently sideways as it waves. Frame order reading left to right then top to bottom: hand tilted slightly outward, hand upright, hand tilted slightly inward, hand upright transitioning back. Keep arm scale consistent: visible arm approximately 360 pixels wide and 250 pixels high, fully inside each cell. It is the same blue mascot arm in each image, with simple chunky pixel clusters and no smoothing. Deliberately redraw the hand/forearm poses on the pixel grid instead of rotating the entire arm. Keep the right shoulder shape and position exactly registered across all four frames. No sleeves with new logos, accessories, weapons, scenery, text, labels, shadows, fake checkerboard or opaque backdrop. Everything outside the four separate arms must be real alpha transparency.

The returned sheet is 1254 x 1254 rather than the requested 1024 x 1024. Import slices use its actual 627-pixel cells. The source is preserved as generated.
