# Rabbit Oil Rig tower

Approved direction (2026-11-08): a cream rabbit wearing a yellow hard hat and navy work overalls, operating a compact navy/ochre fuel extractor. The short pump and elbow pipe feed an integrated fuel tank. A separate three-segment amber gauge fills and brightens on a **one-second loop**, like Radar's console animation. The rabbit, foundation, pump and tank stay still. No projectile or detached fuel pickup effect is needed.

Open [preview.html](preview.html) locally to play/pause the loop or inspect its three states. [fuel-review.png](fuel-review.png) compares the assembled states; [idle-preview.gif](idle-preview.gif) shows the repeating animation. The browser also provides a 96-pixel canvas view for rough readability review. Final camera size remains unverified.

## Unity visual assets

`Assets/Prefabs/Towers/OilRigTowerVisual.prefab` contains:

```text
OilRigTowerVisual       ground mounting origin; local +Y is outward
  Tower                complete stationary tower SpriteRenderer
  FuelStatus           SpriteRenderer + Animator; sprite-only gauge loop
```

Parent the root under the eventual gameplay tower and orient +Y outward from the planet. Both renderers use the Default sorting layer, orders 0 and 1, with Unity's default sprite material. No gameplay scripts, colliders, targeting, resource events or animation events are included. Root motion is disabled.

`Assets/Animations/Towers/OilRig/OilRigFuelIdle.anim` and its controller play low at 0 seconds, filling at 0.35, complete/bright at 0.7, and return to low at 1.0 seconds. Only `FuelStatus`'s `SpriteRenderer.m_Sprite` changes. Animation uses scaled game time (`Animator` Normal update mode); speed changes therefore affect the loop, and time-scale pause stops it. Freeze/disabled state and economy-tick synchronization need future gameplay hookup. This loop communicates ongoing production activity; its bright frame does not award fuel.

## Sources, scale and placement

[assembly.json](assembly.json) records current sources, hashes, runtime paths/GUIDs, slice rectangles, pivots and offsets. All image-space coordinates below are pixel-edge coordinates measured from the top left.

| Part | Setting | Value |
| --- | --- | --- |
| Tower | Source / runtime | `tower-source-v1.png` / `Assets/Art/Towers/OilRig/OilRigTower.png` |
| Tower | Canvas | 1254 x 1254 |
| Tower | Ground mount | (611, 1171) |
| Tower | Pixels per unit | 1400 |
| Tower | Visible bounds at alpha >=16 | (104, 206) through (1163, 1172), exclusive maximum |
| Gauge | Source / runtime | `status-sheet-source-v1.png` / `Assets/Art/Towers/OilRig/OilRigFuelSheet.png` |
| Gauge | Pixels per unit | 3170 |
| Gauge | Slice cells | Three 418 x 418 cells across the top row |
| Gauge | Anchor on tower | (617, 738), baseline center |
| Gauge | Root-relative position | (0.0042857143, 0.3092857143, 0) |
| Gauge | Conservative screen interior | (540, 675) through (695, 740) |

The generated gauge frames have small horizontal offsets; custom pivots align their visible centers and baselines. Keep those slice pivots. All three states fit inside the screen. The six unused sheet cells are not imported. Visible tower width at scale 1 is **0.7564 units**, alongside Radar's approximately 0.7543 and Bomb's approximately 0.76. This comparison does not establish terrain seating or neighboring-tower clearance.

Both source PNGs have real alpha transparency and are copied byte-for-byte into runtime. No source pixels were repainted, rescaled or trimmed. Imports use Sprite/Single for the tower and explicit Multiple slices for the gauge, Point filtering, Clamp wrapping, Full Rect mesh, alpha transparency, no mipmaps, no compression and no NPOT resizing. Previews use only source crops, rigid placement and nearest-neighbor scaling.

Built-in `image_gen` generated the tower using the approved Radar tower and Bomb base as style/character references. The gauge used this new tower as its palette/style reference. Exact prompts and generation mode are saved in [tower.prompt-v1.txt](tower.prompt-v1.txt) and [status.prompt-v1.txt](status.prompt-v1.txt). Current selected sources are v1; unrelated approved artwork was preserved.

## Gameplay handoff

The design row specifies unlock level 1, HP 15, cost 5, fuel **+3/s**, FOV 0, damage 0 and attack type N/A. The user confirmed a one-second production cadence. These values inform the artwork; this task creates no TowerData or economy implementation.

Current code observations, left unchanged:

- `BaseTowerData.ConsumeFuel()` raises `TowerFuelTick(fuelIntake)` every second, including positive configured values. `Tick()` skips `Fire()` when attack type is `None` or fire rate is nonpositive.
- `ResourceTower.Fire()` currently raises `TowerMoneyTick` in both its Money and Fuel branches. Future gameplay work must resolve the intended fuel-generation path and avoid duplicate awards if using the base fuel tick.
- The gauge starts when the visual Animator enables; it is not synchronized with `fuelTimer` initialization or resource events, and it does not automatically reflect freeze/disabled state.

No gameplay scripts, data, scenes, managers or shared prefabs were changed. Resource wiring, tick synchronization, enabled-state handling and final placement remain separate work.

## Offline verification and manual review

Run `python Art/Towers/OilRig/verify_assets.py` from the repository root with Pillow available. It checks source/runtime equality and hashes, alpha, visible bounds/padding, gauge slices/pivots and screen containment, native clip sequence/mapping/loop settings, absence of Transform curves/events/gameplay components, prefab hierarchy/references, imports, file IDs and unique new GUIDs. It rebuilds the assembled PNG/GIF previews without altering source artwork. The assembled three-state review was inspected visually; browser JavaScript syntax was checked offline.

No Unity launch or Unity tests were performed. The team should manually inspect the visual prefab in Unity 6000.6.7f1: confirm materials/imports, the stable one-second screen loop, camera readability, planet seating/foreground occlusion and neighboring-tower clearance. After future gameplay hookup, confirm +3 fuel once per second, disabled/frozen behavior and any desired animation synchronization.
