# Basic Goose poop shot

Approved direction: a compact brown pellet with an ivory bird-poop tip, one static flight sprite, and a small three-frame impact splat. This represents a straight single-target shot. The visual effect stays confined to the struck tower.

Generated with the built-in image-generation tool using the Basic Goose UFO as a style reference. The original transparent sheet is preserved as `goose-poop-sheet-source-v1.png`; `prompt-v1.txt` records the exact prompt. `sprites.json` records the Unity slices, pivots, scale and animation timing. No source pixels were changed during integration.

## Unity assets

- `Assets/Art/Projectiles/GoosePoop/GoosePoopSheet.png`: expand this asset to access `GoosePoopFlight`, `GoosePoopImpact01`, `GoosePoopImpact02` and `GoosePoopImpact03`. The four equal 627 by 627 cells use individual visual-center pivots to compensate for offsets in the generated sheet.
- `Assets/Prefabs/Projectiles/GoosePoopProjectileVisual.prefab`: Transform and SpriteRenderer only. The ivory tip points along local **+X**. Future gameplay should move this object along the shot's straight trajectory and rotate +X toward the destination.
- `Assets/Prefabs/Projectiles/GoosePoopImpactVisual.prefab`: SpriteRenderer plus Animator, using `Assets/Animations/Projectiles/GoosePoopImpact.controller` and `GoosePoopImpact.anim`. The single default state plays the three impact frames at 12 fps, then assigns an empty sprite at 0.25 seconds. It does not loop, move, damage, destroy or return the object to a pool. Future gameplay owns spawning, cleanup and replay when reusing it.

Both visuals use Point filtering, alpha transparency, Clamp wrapping, no compression or mipmaps, Full Rect sprite meshes, 1024 pixels per unit and sorting order 10 on the Default layer. Their default Transform scale is 1. The visible pellet is approximately 0.271 units wide versus the goose UFO's 2.273 visible units at its default scale: roughly one-eighth of the UFO width. The largest splat is approximately 0.285 units wide. These measurements exclude transparent padding. Apply matching scale changes if gameplay scales the UFO.

The impact animation contains only SpriteRenderer sprite keys. The projectile's travel, launch point, firing timing, hit detection and damage remain gameplay work; no C# scripts, animation events or changes to the goose, towers or scenes were added.

## Manual review

Per `AGENTS.md`, no Unity tests or Unity validation runs were performed. Offline checks cover source/runtime equality, transparency, slice boundaries, pivots, scale, GUID references and the animation's frame order and non-looping setting.

In Unity, expand the sheet and inspect the four slices. Drag the projectile beside the goose and confirm visibility through the intended camera. Preview `GoosePoopImpact.anim` on its visual prefab: squash, splat, flecks, then empty. Confirm the hit reads as one small contact point and adjust sorting or the visual scale to suit the gameplay scene.
