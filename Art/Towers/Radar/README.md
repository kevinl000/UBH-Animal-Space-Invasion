# Rabbit Radar tower

Approved direction (2026-11-08): a fixed upward-facing dish with a cream rabbit in a teal communications vest and headset. A separate console indicator pulses; the dish, mast, rabbit and foundation remain stationary. No coverage-sector overlay is included.

Open [preview.html](preview.html) locally to play/pause the console loop or select a frame. [idle-preview.gif](idle-preview.gif) is the assembled animation; [console-review.png](console-review.png) compares its three states. The preview also shows the full canvas at 96 pixels for a rough readability review. Actual camera scale remains unverified.

## Unity assets

`Assets/Prefabs/Towers/RadarTowerVisual.prefab` contains:

```text
RadarTowerVisual             ground origin; local +Y is outward
  Tower                      static complete rabbit/dish/foundation SpriteRenderer
  ConsoleStatus              SpriteRenderer + Animator; sprite-only activity loop
```

Parent the visual root under a future gameplay tower and orient local +Y away from the planet. The dish stays fixed relative to that root; planet rotation can carry the entire tower. There is no aiming pivot or sweep animation. The two renderers use the Default sorting layer, orders 0 and 1, and Unity's default sprite material. Root motion is disabled.

`Assets/Animations/Towers/Radar/RadarConsoleIdle.anim` and its controller play a 1.2-second looping sequence: dim at 0, active at 0.3, contact at 0.5, active at 0.7, dim at 0.9, and the same dim frame at 1.2 seconds. Only the `ConsoleStatus` SpriteRenderer's sprite changes. There are no Transform keys, animation events, scripts, colliders, movement, damage or detection components. This is decorative activity and is not synchronized with real stealth detection or fuel/disabled state.

The README gameplay row specifies unlock level 1, HP 10, cost 5, fuel -1/s, FOV 90 degrees, damage 0 and attack type N/A. This task prepares artwork only and creates no gameplay data. The intended detection sector is **90 degrees total: -45 to +45 degrees from local +Y**, independent of the fixed-dish animation. Numeric detection distance is not inferred from that angle.

## Source, scale and pivots

[assembly.json](assembly.json) records the selected sources, hashes, runtime GUIDs, PPU, slice rectangles, pivots and offsets. Coordinates below are pixel-edge coordinates from the top left of each original image.

| Part | Setting | Value |
| --- | --- | --- |
| Tower | Source / runtime | `tower-source-v1.png` / `Assets/Art/Towers/Radar/RadarTower.png` |
| Tower | Canvas | 1254 x 1254 |
| Tower | Ground mount | (611, 1171) |
| Tower | PPU | 1400 |
| Tower | Visible bounds, alpha >=16 | (105, 154) through (1161, 1172), exclusive maximum |
| Console | Source / runtime | `status-sheet-source-v1.png` / `Assets/Art/Towers/Radar/RadarStatusSheet.png` |
| Console | PPU | 2300 |
| Console | Slice cells | Three 418 x 418 cells across the top row |
| Console | Anchor on tower | (638, 746), baseline center |
| Console | Root-relative position | (0.0192857143, 0.3035714286, 0) |

The tower's visible width at scale 1 is approximately **0.7543 units**, alongside Bomb's approximately 0.76 and Sniper's 0.7693. Apply the intended overall tower scale when mounting. This is a footprint comparison, not planet/socket fitting.

The independent generated status frames have horizontal offsets; custom slice pivots place their baseline centers at the same anchor. Their approximately 263-264 x 113 source-pixel graphics fit wholly within the monitor's empty interior after scaling. Do not replace these pivots with centered full-cell pivots. Imports use Sprite/Single for the tower and Sprite/Multiple for the sheet, Full Rect mesh, Point filtering, Clamp wrapping, alpha transparency, no mipmaps/compression and no NPOT resizing.

Runtime PNGs are byte-identical copies of the selected source files. Generated alpha, including faint low-alpha edge pixels, is preserved; alpha >=16 is the documented bounds/placement threshold. No source artwork was repainted or trimmed. The sheet's six unused cells are not imported as sprites.

## Provenance and verification

Built-in `image_gen` generated both sources with real alpha transparency. [tower.prompt-v1.txt](tower.prompt-v1.txt) records the exact tower prompt, using the existing Sniper/Bomb bases as character/style references. [status.prompt-v1.txt](status.prompt-v1.txt) records the exact indicator prompt, using the new tower as its style/palette reference. These are reference-guided new generations. Unrelated approved artwork and staged Sniper files were preserved.

From the repository root, run `python Art/Towers/Radar/verify_assets.py` with Pillow installed for inexpensive offline checks and to rebuild the assembled PNG/GIF previews. It checks source/runtime hashes, alpha, slice gutters/pivots, all three indicators' containment within the screen, native clip sequence/loop settings, absence of Transform keys/events/gameplay components, prefab internal references, imports, sprite file IDs and new GUID uniqueness. The previews use source crops, rigid placement and nearest-neighbor scaling; they do not modify source or runtime artwork. Browser JavaScript syntax was also checked offline. No Unity launch or Unity tests were performed.

Current `RadarTower.Fire()` checks distance against `data.fov` and does not enforce an angular sector. `BaseTowerData.Tick()` also skips `Fire()` when attack type is `None`; this matters when configuring a non-damaging detector. Those scripts and all gameplay data remain unchanged. Future gameplay hookup must implement the intended angular/distance rules and drive actual reveals; the visual animation itself reveals no enemies. Gamma Radar, coverage overlays, enemy reveal effects and gameplay launch wiring are separate work.

Manual review for the team: open the visual prefab in Unity 6000.6.7f1, confirm Sprite imports/materials and the console loop, then inspect camera readability, terrain seating/occlusion and neighboring tower clearance at the intended planet scale. Confirm the dish stays fixed and the entire assembly follows the parent planet. After separately scoped gameplay hookup, verify the 90-degree sector and disabled-state behavior.
