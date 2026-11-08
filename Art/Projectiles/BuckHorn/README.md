# Restraining Buck horn and frost impact

Built-in `image_gen` produced a new transparent sheet using the approved Restraining Buck sprite for palette/style. Exact prompt: `prompt-v1.txt`. Original RGBA sheet: `buck-horn-sheet-source-v1.png`; runtime copy: `Assets/Art/Projectiles/BuckHorn/BuckHornSheet.png`, byte-identical. Original pixels remain unchanged.

Four equal 627 by 627 cells: ivory/cyan antler flight sprite top-left, impact 1 top-right, impact 2 bottom-left, impact 3 bottom-right. `sprites.json` records Unity rectangles, stable per-slice visual-center pivots and file IDs. Import is Multiple Sprites, Point, Clamp, Full Rect, alpha transparency, no compression/mipmaps/NPOT resizing, 881.25 PPU.

## Visual assets

- `Assets/Prefabs/Projectiles/BuckHornProjectileVisual.prefab`: Transform and SpriteRenderer only. Natural ivory/tan antler shaft with cyan frost tips points local +Y; visible length 0.48 units and width approximately 0.252 units at scale 1. Current EnemyProjectile uses +Y inward, so no child rotation is needed. Motion, spawning and hit detection are not provided.
- `Assets/Prefabs/Projectiles/BuckHornImpactVisual.prefab`: Transform, SpriteRenderer and Animator. Three compact chip frames, 12 fps, sprite keys at 0, 1/12 and 2/12 seconds; blank at 0.25 seconds. The largest visible width is approximately 0.313 units. This is a single contact effect, not an AoE cue.
- Clip/controller: `Assets/Animations/Projectiles/BuckHorn/BuckHornImpact.anim` and `.controller`. Default state is `Impact`; non-looping, only SpriteRenderer.m_Sprite keys, no Transform/float curves, root motion or animation events.

Future gameplay owns movement, scale, spawn offset from the buck's Muzzle, damage, effect placement, replay and cleanup/pooling. Keep the projectile independent of the boss visual scale. Reusing the effect requires restarting Impact at time zero; the final blank frame does not destroy or despawn its GameObject. These visual-only prefabs cannot be assigned directly to fields typed as EnemyProjectile.

Freeze is separate from the horn's damage/impact. Current boss code starts freeze immediately when firing; no timing or duration was changed. Place the separate frozen-status frame on the actual frozen tower and remove it when that tower unfreezes. See `../../Effects/BuckFreeze/README.md` and `../../Enemies/RestrainingBuck/README.md`.

Preview and offline verification are in `Art/Enemies/RestrainingBuck/`. No Unity launch or Unity tests were performed. Manually check +Y orientation, small-camera readability, launch clearance, impact placement and pooled replay after gameplay hookup.
