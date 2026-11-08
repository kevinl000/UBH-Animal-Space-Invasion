# Shielded Skunk artwork

The approved crimson Basic Skunk 3 under the same thin cyan, spherically shaded transparent shield as Shielded Goose. Existing body, membrane and rim textures are referenced directly with their original GUIDs; no new generation, repainting or duplicate PNG imports are needed.

## Deliverables

- `Assets/Prefabs/Enemies/ShieldedSkunkVisual.prefab`: visual-only assembly using the existing sprites.
- `preview.html`: original/shielded comparison, UFO width slider, background selection and blocked-hit visibility preview. Open locally without a server.
- `contact-review.png`: assembled 80, 160 and 240-pixel UFO widths on dark/light backgrounds.
- `assembly.json`: source references, native transforms, opacity, facing and bomb-effect references.
- `verification.json`: offline measurements, reference checks and remaining manual review.

## Shared shield and assembly

```text
ShieldedSkunkVisual
  Body               existing crimson BasicSkunk3 sprite; order 0
  Shield             independently removable group, fitted around the body
    Membrane         existing ShieldedGoose/Shield.png; order 1; alpha 0.32
    Rim              existing ShieldedGoose/ShieldRim.png; order 2; alpha 1
```

The shield's textures, thin rim, stepped spherical shading, internal child settings and approximately 32% center opacity match the approved goose. Only the parent Shield group's uniform scale and position change to fit the skunk's visible bounds. The body retains its existing PPU, centered pivot, scale 1 and identity rotation. Exact dimensions and transforms are in `assembly.json`.

Generation provenance and exact original prompts remain at `../ShieldedGoose/prompt-v3.txt` and `../ShieldedGoose/prompt-rim-v3.txt`. The reused textures retain their existing Point/Clamp, alpha-enabled Sprite imports with no mipmaps, compression or resizing. Keep the shared Shielded Goose texture assets when using this prefab; their folder name does not restrict them to geese.

## Gameplay handoff

The README row and existing Shielded Skunk data specify 8 HP, 2 money, 2 fuel and 2 AoE damage. Its shield negates the first ordinary incoming hit. Data and runtime code are unchanged. The prefab contains only Transforms and SpriteRenderers; it cannot be assigned directly to EnemyData's typed gameplay-prefab field.

Future gameplay must disable the whole `Shield` group when `HasShield` is consumed, leave it available for shield-bypassing damage, and restore visibility from the initialized state when pooled enemies are reused. Current `BaseEnemyData` already consumes the shield and calls the empty `OnShieldBroken()` hook; this task adds no runtime hookup. A break animation is optional; no new flight or break frames are needed.

Pilot faces local -X and the empty central bomb outlet points -Y. Current gameplay roots face +Y inward, so the future visual child needs local Z rotation 180 degrees. Reuse `SkunkBombProjectileVisual.prefab`, `SkunkBombBurstVisual.prefab` and `SkunkBombHitPuffVisual.prefab` under `Assets/Prefabs/Projectiles/`. The bomb already faces +Y and needs no rotation under the projectile root. Gas grows +Y outward at impact. The existing angular AoE includes targets within 30 degrees on each side; show small puffs only on additional actual hit recipients. No new blast radius, fixed neighbor count or damage-over-time behavior is introduced.

## Review

Offline checks cover unchanged shared assets, prefab GUID/references and visual-only components, inherited shield settings, whole-body membrane coverage, fitted spherical containment, body/rim clearance and preview references. Alpha >=128 defines the measured silhouette; the inherited thin stair-step rim and faint edges still need visual review through the intended camera.

No Unity launch or Unity tests were run. In Unity, inspect imports/materials/sorting and camera readability, toggle `Shield`, and check pilot/tail, thrusters and the empty bomb outlet remain readable. After gameplay hookup, review first-hit blocking, bypassing hits, pooled resets, inward orientation, bomb spawn alignment and gas placement on actual damage recipients.
