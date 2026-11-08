# Radar scan and enemy reveal effects

Approved direction (2026-11-08): a brief expanding mint **90-degree scan arc**, plus a detached four-corner **detection marker** on newly revealed enemies. Enemies stay revealed permanently; only the marker fades. The scan interval remains undecided. The dish stays fixed.

Open [preview.html](preview.html) locally and press **Scan now**. The faint demo enemy becomes visible and stays visible after its marker clears. Further scans show the dish pulse alone until **Reset demo stealth** is used. **Repeat scans** and **Preview interval** let the team explore timing; they change only the browser demonstration. The default 2.5-second preview value is not a gameplay decision. [reveal-preview.gif](reveal-preview.gif) shows one reset/reveal demonstration per GIF cycle; its restart does not mean enemies regain stealth. [effect-review.png](effect-review.png) compares all six source frames with their actual pivots aligned.

## Unity assets and timing

These files contain only Transform, SpriteRenderer and Animator:

| Visual prefab | Clip and controller under `Assets/Animations/Effects/RadarReveal/` | Sprite keys | Ends blank |
| --- | --- | --- | --- |
| `Assets/Prefabs/Effects/RadarScanPulseVisual.prefab` | `RadarScanPulse` | 0, 0.12, 0.24 seconds | 0.36 seconds |
| `Assets/Prefabs/Effects/RadarRevealMarkerVisual.prefab` | `RadarRevealMarker` | 0, 0.08, 0.16 seconds | 0.24 seconds |

Both clips are **one-shot**, with a final empty sprite and no loop. They animate only `SpriteRenderer.m_Sprite`, with no Transform/color keys, animation events or root motion. They do not schedule scans, reveal enemies, track targets or despawn their GameObjects. Future gameplay must trigger/restart the clips on each reuse and clean up or pool the objects after completion. Do not create an always-looping effect to represent a gameplay interval that has not been configured.

The effects use the Default sorting layer/order 10 and Unity's default sprite material. Tune sorting to the actual scene. The source sheet is copied byte-for-byte to `Assets/Art/Effects/RadarReveal/RadarRevealSheet.png`; its import uses Sprite/Multiple, 700 PPU, custom pivots, Full Rect mesh, Point filtering, Clamp wrapping, alpha transparency, no mipmaps/compression and no NPOT resizing.

## Placement and scale

The arc points local **+Y**, with visible pixels within -45 to +45 degrees of outward. Place its origin at the dish receiver tip and orient it with the fixed tower. For the current `RadarTowerVisual` at scale 1, the receiver tip is at root-relative **(0.1907142857, 0.7264285714, 0)**. This comes from tower-source pixel (878, 154), ground mount (611, 1171), and 1400 tower PPU. A child at that local position/identity rotation inherits the whole tower's scale and planet rotation.

The three source arcs were generated at slightly different offsets. Their measured emission anchors are (223.5, 404.5), (206.5, 417.5), and (200, 430) relative to each slice's top left. Custom pivots align these to one stationary origin. Scan slices are 418 x **500** pixels, extending into the transparent gap below the top row so that all three anchors lie inside their slices. That extension contains no pixels at alpha >=16 and includes no marker artwork. The marker slices are the three 418 x 418 cells in the middle row. The unused bottom row is not imported.

At alpha >=16, every scan pixel falls within the intended 90-degree sector. The measured angular extrema range from approximately +/-44.4 to +/-44.85 degrees, with stepped pixel ends and stroke thickness. These are stylized crests, not exact mathematical circles or gameplay hit boundaries. Their center-top distances from the emission point grow from approximately **0.221 to 0.341 to 0.446 units** at scale 1. This is a short near-dish emission cue; it does not depict or determine the actual detection distance. No filled cone or permanent coverage overlay is included.

Place the marker at the center of the rendered enemy artwork and let it follow that visual while it plays. Its custom pivots center all four corners despite generated frame offsets. The prefab has a fixed local X/Y scale of **1.75**, normalizing the marker to approximately 0.71-0.75 units wide by 0.47-0.50 units high. This is an initial art size; fit it around the final enemy visual bounds during hookup. Do not size it from a transparent full-image canvas or assume the gameplay root is the visual center. The preview uses the current Goose artwork as a size/readability reference, not a newly designed stealth variant.

[sprites.json](sprites.json) records the source hash, runtime GUIDs, slices, pivots, durations, prefab scales and attachment coordinates. `scan_interval_seconds` is deliberately `null`.

## Gameplay handoff

The intended hookup is: on each configured scan, play the dish pulse; obtain the living stealthed enemies in the real detection range and +/-45-degree sector; reveal them; play one marker on each enemy whose state actually changed. Already revealed enemies stay visible and receive no repeated newly-revealed marker. The scan pulse can play even when no enemy is found. The visual crest is informational, not a moving damage/reveal projectile, so no wait for a crest collision is implied.

Current `RadarTower.Fire()` uses distance against `data.fov` without an angular check. `BaseTowerData.Tick()` also skips towers with `attackType == None`. `RevealStealth()` clears stealth permanently, which matches the approved behavior. These scripts, gameplay data, tower prefab, enemy prefab, scenes and managers were left unchanged. Proper interval scheduling, angular/range filtering, disabled/frozen behavior, visual reveal-state binding, pooling/replay and gameplay-triggered effects remain separate implementation work. No interval, numeric detection distance or new reveal duration was invented here.

## Generation and checks

Built-in `image_gen` generated `radar-reveal-sheet-source-v1.png` using the existing Radar tower only as a palette/pixel-art reference. [prompt-v1.txt](prompt-v1.txt) preserves the exact prompt and generation mode. Source pixels and alpha were preserved; no code repainting or source replacement was used. Faint generated alpha below 16 is retained; bounds and geometry checks use alpha >=16.

Run `python Art/Effects/RadarReveal/verify_assets.py` from the repository root with Pillow installed to check source/runtime hashes, alpha, all six slice gutters/pivots, every visible scan pixel's angular containment, increasing crest distances, empty marker centers, clip keys/final blanks, loop flags, imports, prefab references, metadata pairing and asset GUID uniqueness. It also rebuilds the assembled PNG/GIF previews by cropping, rigid placement and nearest-neighbor scaling. The detailed angle report is local at `.git/menu-work/radar-reveal/verification.json`. Browser script syntax was checked offline. **No Unity launch or Unity tests were performed.**

Manual review for the team: inspect the two visual prefabs and all six slices in Unity 6000.6.7f1. Check the scan origin on the receiver tip, outward direction, final-camera pixel readability, scene sorting, and marker fit/clear center. After gameplay hookup, confirm repeated and empty scans, one marker per newly revealed enemy, permanent visibility after the marker ends, pause/disabled behavior and pooled replay with no stale frame.
