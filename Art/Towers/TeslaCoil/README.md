# Rabbit-operated Tesla Coil

Approved direction, 2026-11-08: a cream electrician rabbit with copper-framed cyan goggles, teal jacket, navy/ivory machinery and a cyan operator screen. A separate emitter has three copper windings, an ivory ceramic core, cyan electrode and attached round hinge disk.

Open [preview.html](preview.html) locally to aim or play the **120-degree sweep, local Z -60 to +60 degrees**. [sweep-review.png](sweep-review.png) shows five assembled poses. Only the emitter rotates; the rabbit, controls, seat, support and foundation remain fixed. The optional markers show the pivot and outgoing emission centerline.

## Attack intent and scope

The user confirmed a **localized electric burst that damages every enemy in the affected area**, aimed within the 120-degree sweep. There is no fixed recipient count or capped chain. The whole 120-degree aiming sector is not the burst footprint. Burst radius, range and cadence remain undecided; no numeric area is inferred from the FOV. The separate [Tesla output assets](../../Projectiles/RabbitTesla/README.md) now provide a traveling electric bolt, electrode flash and localized burst. Open their [interactive preview](../../Projectiles/RabbitTesla/preview.html) to review a shot; gameplay remains unconnected.

The main README row lists unlock level 3, HP 25, cost 15, fuel -3/s, FOV 120 degrees, damage 2 and AoE. This is twice Bomb's listed damage of 1; a larger radius or faster fire rate is not assumed. These assets add no gameplay data or implementation.

Current `ShootingTower.cs` treats `data.fov` as a distance and spawns shots at the tower root; it does not implement this angular aiming clamp or emission-marker hookup. Current `TowerProjectile.cs` faces local +X and tracks its target; current `AoETower.cs` applies splash around the primary target with a default `effectRadius` of 2. Those scripts were read and left unchanged. That default is not an approved Tesla radius. Future integration needs to resolve the distance/angle distinction, aim the visual pivot, use the emission marker and apply effects to the actual hit set.

## Unity visual prefab

`Assets/Prefabs/Towers/TeslaCoilTowerVisual.prefab` contains only Transform and SpriteRenderer components:

```text
TeslaCoilTowerVisual        ground mounting origin; local +Y points outward
  Base                     stationary rabbit, screen, support and foundation
  CoilPivot                independently rotating emitter; neutral points +Y
    Emission               electrode tip; local +Y is the outgoing direction
```

Render orders are 0 for Base and 1 for the emitter, on the Default sorting layer, using Unity's default sprite material. There is no Animator, automatic sweep, aiming script, collider, firing, damage, pooling or scene wiring. Gameplay must enforce the +/-60-degree clamp relative to the planet-facing root. This visual prefab is intended as a child of a future gameplay prefab.

## Sources and setup

Both original PNGs are 1254 x 1254 with real alpha. Runtime files are byte-identical copies; no source pixels were repainted, trimmed or resampled. The built-in `image_gen` tool made the new base using the current Bomb base as its family/layout reference, then generated the emitter using the new Tesla base as its palette/style reference. No unrelated approved art was changed. Exact prompts are [base.prompt-v1.txt](base.prompt-v1.txt) and [emitter.prompt-v1.txt](emitter.prompt-v1.txt); current sources are **base-source-v1.png** and **emitter-source-v1.png**.

[assembly.json](assembly.json) records source hashes, runtime paths/GUIDs, positions and verification. Image points use top-left pixel-edge coordinates:

| Setting | Value |
| --- | --- |
| Base PPU / ground mount | 1500 / (582, 1204) |
| Base socket | (1091.5, 478) |
| Emitter PPU / hinge | 2200 / (626.5, 1064) |
| Emitter emission tip | (626.5, 88) |
| CoilPivot root-relative position | (0.3396666667, 0.484, 0) |
| Emission hinge-relative position | (0, 0.4436363636, 0) |

The base's alpha>=128 footprint is approximately **0.765 units wide**, comparable to Bomb's 0.76 at source scale. The full swept envelope is larger. These dimensions do not establish planet/socket fit. Imports use custom ground/hinge pivots, Point filtering, Clamp wrapping, Full Rect, alpha transparency, no mipmaps, no compression and no NPOT resizing.

## Offline verification and manual review

Run `python Art/Towers/TeslaCoil/verify_assembly.py` with Pillow for offline geometry/reference checks and a rebuilt assembled review PNG. It samples 241 angles in 0.5-degree steps, alpha>=128 and every fourth emitter source pixel. Results: zero protected overlaps, continuous sampled joint contact, and zero base intersections along a two-unit outgoing centerline. A 0.12-unit region around the hinge permits intended overlap with the socket/upper support; the operator screen remains protected. This is sampled raster geometry, not a continuous proof or clearance for an arbitrary-width effect.

The verifier also checks source/runtime equality, transparent padding, native prefab hierarchy and references, custom pivots/PPU, import settings, new GUID uniqueness and local preview image references. JavaScript syntax is checked when Node is available. Preview images use rigid placement/rotation and nearest-neighbor scaling only; source/runtime artwork stays unchanged.

No Unity launch or Unity tests were performed. In Unity, manually inspect import/material appearance, neutral/intermediate/endpoint poses, emission alignment, final camera readability, terrain seating and neighboring-tower clearance. The browser preview's rendering has not been exercised in an automated browser. Effects, effect replay and damage require a later scoped hookup.
