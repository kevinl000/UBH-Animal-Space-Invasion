# Rabbit Bank tower

Approved direction (2026-11-08): cream rabbit banker in a navy waistcoat, ivory shirt and green tie, operating a compact closed vault. Emerald and brass accents distinguish the Bank from the Oil Rig. The rabbit, controls, vault and foundation remain stationary; only a separate coin display pulses on a **one-second loop**. No projectile, floating cash pickup or opening-vault animation is included.

Open [preview.html](preview.html) locally to play/pause the loop or inspect each state. [coin-review.png](coin-review.png) compares the three assembled states; [idle-preview.gif](idle-preview.gif) shows the animation. The browser also includes a 96-pixel canvas view; actual camera readability needs manual review.

## Unity visual assets

`Assets/Prefabs/Towers/BankTowerVisual.prefab` contains:

```text
BankTowerVisual         ground mounting origin; local +Y is outward
  Tower                 stationary complete Bank SpriteRenderer
  CoinStatus            SpriteRenderer + Animator; sprite-only money activity
```

Parent the visual root under a future gameplay tower, with +Y pointing away from the planet. SpriteRenderers use the Default sorting layer, orders 0 and 1, and Unity's default sprite material. No gameplay scripts, colliders, resource awards or animation events are included. Root motion is disabled.

`Assets/Animations/Towers/Bank/BankCoinIdle.anim` and its controller show three illumination states at 0, 0.35 and 0.7 seconds, returning to the first at 1 second. The clip swaps only `CoinStatus`'s `SpriteRenderer.m_Sprite`. Normal Animator update mode follows scaled game time, including time-scale pause/speed changes. The loop indicates activity; it is not synchronized with actual money awards or freeze/disabled state.

## Sources and setup

[assembly.json](assembly.json) records exact source/runtime paths, hashes, GUIDs, image sizes, ground mount, pixels per unit, slices, alignment pivots, screen bounds and local offsets. Image coordinates are pixel-edge coordinates from the top left. Keep the custom pivots; they align generated frames despite small source offsets. The other six sheet cells are not imported.

Tower PPU is 1340, with ground mount (610, 1169) on the 1254 x 1254 source canvas. Display PPU is 2730; its three 418 x 418 slices use baseline-center anchor (620, 736), or local position (0.0074626866, 0.3231343284, 0). Visible tower width at scale 1 is approximately **0.7515 units**, close to Oil Rig's 0.7564 and Radar's 0.7543. This is a footprint comparison, not a terrain-fit guarantee.

Runtime PNGs are byte-identical copies of `tower-source-v1.png` and `status-sheet-source-v1.png`. Generated alpha is preserved; no source pixels are repainted, trimmed or rescaled. Imports use Sprite/Single for the tower and explicit Multiple slices for the display, Point filtering, Clamp wrap, Full Rect mesh, alpha transparency, no mipmaps, no compression and no NPOT resizing. Preview renders use crops, rigid placement and nearest-neighbor scaling only.

Built-in `image_gen` generated the tower using Oil Rig and Radar as style/character references, then generated the display using the new Bank sprite as a palette/style reference. Exact prompts and generation mode are preserved in [tower.prompt-v1.txt](tower.prompt-v1.txt) and [status.prompt-v1.txt](status.prompt-v1.txt). Selected sources are v1. Existing artwork and GUIDs remain unchanged.

## Gameplay handoff

The README row specifies unlock level 1, HP 15, cost 5, fuel **-1/s**, FOV 0, damage 0 and attack type N/A. The user confirmed **$3 generated every second**. These are design context; this task does not create TowerData or implement economy behavior.

Current `ResourceTower` defaults to `generationAmount = 3`; its Money branch raises `TowerMoneyTick(generationAmount)` when `Fire()` is called. However, `BaseTowerData.Tick()` skips `Fire()` for attack type `None` or nonpositive fire rate. Future hookup must provide a noncombat resource cadence instead of assuming this visual prefab produces money. `BaseTowerData.ConsumeFuel()` already emits the configured fuel amount once per second, separately from `Fire()`. The existing Fuel branch in `ResourceTower.Fire()` raises a money event too; it was left unchanged.

The visual Animator starts on enable and does not automatically follow resource timers or disabled/frozen state. Scheduling, resource configuration, tick synchronization and state handling remain gameplay work. No scripts, data, scenes, managers or shared prefabs were edited.

## Verification and manual review

Run `python Art/Towers/Bank/verify_assets.py` from the repository root with Pillow installed. It checks source/runtime hashes and equality, alpha, source bounds, slice padding/pivots and screen containment, import settings, sprite IDs, unique new GUIDs, prefab hierarchy/offsets/references and sprite-only one-second clip keys/mapping. It rebuilds the assembled PNG/GIF without changing source art.

No Unity launch or Unity tests are used. The team should inspect the prefab in Unity 6000.6.7f1 for import/material appearance, stable screen animation, final-camera readability, planet seating/occlusion and neighboring-tower clearance. After future gameplay hookup, check exactly $3 per second, -1 fuel per second, pause/speed behavior and freeze/disabled handling.
