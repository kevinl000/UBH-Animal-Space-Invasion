# Basic Goose 1 artwork

A Canada goose piloting a blue UFO, based on `Assets/Art/MainMenu/MainPage-ufo.png`. Generated with the built-in image generation tool; the exact prompt is in `prompt-v1.txt`.

- `basic-goose-1-source-v1.png`: original generated RGBA artwork, preserved unchanged.
- `Assets/Art/Enemies/BasicGoose1/BasicGoose1.png`: identical runtime sprite, with its own Unity GUID.
- `Assets/Prefabs/Enemies/BasicGoose1Visual.prefab`: visual-only GameObject containing a Transform and SpriteRenderer.

## Using it in Unity

Drag `BasicGoose1Visual.prefab` into a 2D scene or under the teammate's enemy root. It uses a world-space SpriteRenderer, not a Canvas Image. The goose faces left; use SpriteRenderer Flip X for the opposite direction. Keep the artwork upright unless gameplay requires rotation.

The PNG is 1254 by 1254 with a transparent background. Import settings use Single Sprite, Point filtering, Clamp wrap, no mipmaps, no compression, Full Rect mesh and a centered pivot. At 512 pixels per unit, its full canvas is approximately 2.449 units wide. This is a convenient starting art scale, not a confirmed gameplay size; adjust the visual Transform to fit the gameplay camera. The renderer uses Unity's default sprite material and the Default sorting layer at order 0; set ordering to suit the gameplay scene.

This is one static sprite including its thruster. Movement, hovering, projectile art, firing, collision, HP, rewards, shiny variants and animation are not supplied. Existing menu artwork, scenes, managers and enemy scripts are unchanged.

## Verification

The PNG's alpha channel was inspected and the source/runtime copies compared. Offline checks cover metadata, GUID references and the prefab's visual-only components. An isolated Unity 6000.6.7f1 check was attempted, but the editor crashed during startup: Windows could not allocate its 1 GB UDS shared-memory block. No Unity import, rendering or Play Mode check passed. The local helper and crash log are under `.git/menu-work/basic-goose-unity/` and are not game assets.

Manual check: let Unity import the new assets, drag the prefab into a 2D scene, confirm the sprite displays with transparency and no missing references, then inspect its size and readability through the gameplay camera. Check Console for import or material errors and adjust visual scale/sorting as needed. Save any test placement only in the intended gameplay scene.
