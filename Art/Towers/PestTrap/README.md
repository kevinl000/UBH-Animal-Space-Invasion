# Rabbit-operated Pest Trap tower

Current art: **base-source-v2.png** and **launcher-source-v1.png**. The rabbit wears teal pest-control workwear and amber goggles, with both ears exposed. A small teal/mint monitor on the stationary diagonal support faces the rabbit below the weapon hinge, supplementing the joystick with a simple status display. The moving weapon has a narrow single nozzle and a large amber resin reservoir, distinguishing it from the Laser and Bomb towers. The operator, screen, controls and chassis stay fixed; the complete launcher and reservoir rotate together.

Screen revision (2026-11-08): a targeted edit adds the support-mounted monitor. Superseded base v1 is archived locally outside the active art folder; existing pivots, PPU, import metadata/GUIDs, launcher artwork and prefab remain unchanged.

Open [preview.html](preview.html) locally to aim the exact source sprites or play a demonstration sweep. [sweep-review.png](sweep-review.png) shows five poses. The preview uses the same measured pivots, PPU and offsets as the visual prefab; positive Unity Z rotates counterclockwise. It requires no server or external libraries.

## Visual prefab

`Assets/Prefabs/Towers/PestTrapTowerVisual.prefab` contains only:

```text
PestTrapTowerVisual          ground mounting origin; parent to the planet
  Base                      stationary rabbit, controls, chassis, support and screen
  CannonPivot               freely rotating launcher and reservoir
    Muzzle                  attached firing reference, local +Y out of nozzle
```

Only `Base` and `CannonPivot` have SpriteRenderers. The base draws at order 0, the launcher at order 1, using Unity's default sprite material. There are no scripts, colliders, Animator, damage, targeting, firing or automatic sweep behavior. Gameplay must drive `CannonPivot` and clamp its local Z angle to **-60 through +60 degrees** relative to the planet's outward direction. Neutral root and launcher rotations are identity, firing local **+Y**.

Runtime sprites are byte-identical copies of the selected source PNGs:

- `Assets/Art/Towers/PestTrap/PestTrapBase.png`
- `Assets/Art/Towers/PestTrap/PestTrapLauncher.png`

Both use Sprite/Single, custom pivots, Point filtering, Clamp wrapping, alpha transparency, Full Rect mesh, no mipmaps, no compression and no NPOT resizing. Original 1254 x 1254 canvases and alpha are preserved. Separate PPU values normalize the independently generated images. The base's visible alpha >=128 footprint is approximately **0.761 units wide** at scale 1, similar to Bomb's 0.76 and Laser's 0.70; final planet scale is unconfirmed. The full aiming envelope extends beyond that footprint.

## Assembly measurements

Coordinates are pixel-edge coordinates from the top-left of each source PNG. Exact settings, source hashes, asset GUIDs and check results are recorded in [assembly.json](assembly.json).

| Part | Setting | Value |
| --- | --- | --- |
| Base | Ground mount | (600, 1204) |
| Base | Socket center | (1090, 480) |
| Base | Pixels per unit | 1500 |
| Launcher | Hinge center | (627, 1021.5) |
| Launcher | Muzzle reference | (627, 216) |
| Launcher | Pixels per unit | 2100 |
| CannonPivot | Local position from ground mount | (0.326666667, 0.482666667) |
| Muzzle | Local position from launcher hinge | (0, 0.383571429) |

The muzzle marker lies on the nozzle's outer top edge, ready to emit outward without starting behind the lip. The dark opening is illustrated inset in the nozzle's front face. The round breech intentionally covers the socket; a small socket rim remains visible.

## Scope and gameplay handoff

The README design row specifies Tutorial unlock, HP 20, cost 2, fuel -2/s, FOV 120 degrees and sticky substance slowing enemies and dealing 1 damage/s over three seconds. These are context for the artwork; this task does not modify tower data or implement those mechanics. No projectile, splat or persistent coating is baked into the tower. Separate amber glob, nozzle-spurt and short hit-splat visuals are now available in [RabbitPestTrap](../../Projectiles/RabbitPestTrap/README.md); gameplay hookup and any persistent coating remain future work.

Current `PestTrapTower.OnHitEnemy` applies a three-second, 0.5-factor slow and an immediate damage tick; it does not schedule the full three-second DOT. Current `ShootingTower` spawns projectiles at the gameplay root and uses `data.fov` as a distance comparison, with no rotating weapon or angular clamp. These observations are handoff notes, not changes made here. Future gameplay hookup must use the muzzle, reconcile targeting/FOV and implement DOT as separately authorized. `TowerProjectile` currently faces local +X; this tower's muzzle points local +Y, so a future projectile visual must follow the projectile root's own convention. The visual prefab has no `TowerProjectile` component and cannot directly fill a field typed as that component.

## Provenance and offline checks

Built-in imagegen generated both original sprites using the approved Laser portrait and Bomb base as style references. Exact prompts are [base.prompt-v1.txt](base.prompt-v1.txt) and [launcher.prompt-v1.txt](launcher.prompt-v1.txt); the launcher also used the new base as a reference. The screen-equipped base v2 is a targeted built-in imagegen edit of base v1; its exact prompt is [base-screen.prompt-v2.txt](base-screen.prompt-v2.txt). The edit preserves the layout visually and retains the measured socket/ground placement; pixel-identical preservation through generation is not claimed. Exact prompts remain as provenance; historical v1 references in prompts are not rebuild dependencies. The superseded no-screen base is preserved locally at `.git/menu-work/tower-screen-update/superseded/PestTrap/base-source-v1.png` and is not part of the committed assets. Current selected source pixels are copied unchanged into runtime. Preview images apply rigid transforms and nearest-neighbor scaling only.

Run `python Art/Towers/PestTrap/verify_assembly.py` from the repository root with Pillow available. It checks alpha, source/runtime byte equality, saved hashes, import settings, pivots, GUID uniqueness against imported asset metadata, visual-only prefab component types, internal/external references and transform offsets. It also rebuilds the five-pose review and records geometry measurements in the manifest and `.git/menu-work/pest-trap-tower/geometry-samples.json`.

The aiming review passed **241 poses at 0.5-degree intervals** again with the screen-equipped base, using alpha >=128 and launcher samples every four source pixels. There were zero sampled overlaps with the protected base/operator outside a **0.115-unit joint exclusion**, positive hinge contact at every pose, and zero base intersections along the outgoing muzzle centerline sampled every base-source pixel for two world units. The screen's conservative image-space bounds in the manifest are always protected from overlap, including near the joint exclusion. The exclusion covers the intended round breech/socket and upper support; it does not cover the rabbit or lower chassis. The five rendered poses were inspected visually.

These are sampled raster checks, not a continuous collision proof. A clear centerline does not establish clearance for arbitrary projectile width. Planet/socket seating, neighbors, materials and final camera readability remain unverified. No Unity launch or Unity tests were performed. No scenes, gameplay scripts, data, managers or unrelated artwork were edited.

In Unity, manually inspect the prefab at neutral, +/-60 degrees and intermediate poses. Confirm the rabbit stays fixed, the launcher remains seated, and `Muzzle` follows the nozzle. Check sprite/material imports, final camera readability, terrain seating, foreground occlusion, neighboring towers and eventual projectile-width clearance before gameplay integration.
