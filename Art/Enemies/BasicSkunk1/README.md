# Basic Skunk 1 artwork

One black-and-white skunk piloting a compact charcoal/navy UFO, with a blue dome, six mint-green running lights, a small empty central bomb outlet and two small side thrusters. The skunk faces left and its striped tail remains visible inside the dome. The shape and pixel-art style follow Basic Goose 1, while the palette and underside identify the skunk's bomber role.

Generated with the built-in image-generation tool using `Assets/Art/Enemies/BasicGoose1/BasicGoose1.png` as the style/proportion reference. `prompt-v1.txt` records the exact prompt. The original RGBA image is preserved unchanged as `basic-skunk-1-source-v1.png`.

## Unity assets

- `Assets/Art/Enemies/BasicSkunk1/BasicSkunk1.png`: byte-identical runtime sprite with its own GUID. Single Sprite, Point filtering, Clamp wrapping, alpha transparency, no compression or mipmaps, Full Rect mesh, centered pivot and 512 pixels per unit.
- `Assets/Prefabs/Enemies/BasicSkunk1Visual.prefab`: Transform and SpriteRenderer only, Unity's default sprite material, Default sorting layer and order 0. Drag into a 2D scene or use as the visual child of a future gameplay enemy prefab.

The canvas is 1254 by 1254, or approximately 2.449 units square at scale 1. Visible alpha >=128 spans 1171 pixels in width (about 2.287 units), versus the goose UFO's 1164 pixels (about 2.273 units). The skunk is therefore within 1% of the goose's visible width at the same Transform scale. Final gameplay size still depends on the team's camera and enemy setup.

This is one static sprite. The pilot faces local -X; the bomb outlet points local -Y. Keep that outlet aimed toward the intended drop destination when integrating the visual under a gameplay root. `BaseEnemyData` assumes its root's local +Y faces the planet, so a visual child under that convention needs a 180-degree local Z offset to make its outlet face inward. The supplied standalone visual remains upright with identity rotation. No enemy-root orientation or gameplay prefab was changed.

The opening is drawn empty and leaves the central downward path clear of the two thrusters. Bomb art, bomb release animation, spawn markers, collision, enemy stats, movement and firing hookup are not part of this asset. No existing goose/tower art, enemy data, scenes, scripts or prefabs were changed. This work was prepared on `main` at the user's request.

## Manual review

No Unity tests or Unity validation runs were performed, per `AGENTS.md`. Offline checks cover RGBA transparency, source/runtime equality, import settings, GUID/file-ID references, the prefab's visual-only components and relative width against the goose.

Let Unity import the new files, then drag `BasicSkunk1Visual.prefab` beside the goose at equal scale. Confirm the face, tail stripe and empty bomb opening remain readable through the gameplay camera, transparency renders correctly and Console shows no missing references/material errors. When attached to gameplay later, confirm the bomb outlet faces the planet and remains clear of the side thrusters.
