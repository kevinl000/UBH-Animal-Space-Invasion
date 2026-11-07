# Davis Nova visual test

Open `C:/UBHackathon2026` through Unity Hub with **6000.6.7f1**. Let assets import,
open `Assets/Scenes/DavisNova.unity`, and press **Play**. No setup command is needed.

The bright pixel sun and technological ring remain separate sprites. Twelve
interactive sites replace the old inactive `TowerFitPreview` group.

## Controls

- Select **Laser** (selected initially), then hover over a socket to preview it.
- **Left-click** the socket to place a free Laser; **right-click** to remove it.
- Hold **Q / E** to aim all guns, clamped to -60 / +60 degrees. **R** resets aim.
- Re-enter the scene to clear all placements. Other tower cards remain inspectable.

This uses Union Prime's visual sandbox: no combat or resource spending.

## Art review

Check empty and occupied upper, side and upside-down sockets. Foundations should
sit behind the socket fronts; aiming should rotate only the guns. Check the
sun/ring transparency, HUD clearance and pointer placement at 1280x720,
1672x941, 1280x960 and 2560x1080. Return to LevelSelect and re-enter the scene.

## Rebuild

Run `python Art/Planets/DavisNova/prepare_unity.py` from the repository root.
It preserves sprite GUIDs and prepares the interactive prefab again.
See `unity-import.json` for source hashes, scale and twelve mounts.

## Verification, 2026-11-07

Unity **6000.6.7f1** compiled and ran an isolated copy of the current Assets,
Packages and ProjectSettings: **10 passed, 0 failed**. This includes six placement
and pointer tests across Union Prime, Capen Major and Davis Nova, three existing
planet-page navigation/inspection tests, and one temporary render-capture check.
Placement tests cover all twelve sites, duplicate rejection, selection, removal,
aim bounds, fixed foundations, UI blocking and fresh scene entry.

Local results: `.git/menu-work/planet-visual-tests.xml` and
`.git/menu-work/planet-visual-tests.log`. Both importers also passed repeatability,
source-hash, import-setting and prefab-reference checks.

Rendered empty/occupied and +/-60-degree views were reviewed, with occupied
captures at 1672x941, 1280x720, 1280x960 and 2560x1080. They are local in
`.git/menu-work/planet-visual-captures/`. No standalone build or interactive human
Game-view review is claimed. Use the controls above for the team's visual review.
