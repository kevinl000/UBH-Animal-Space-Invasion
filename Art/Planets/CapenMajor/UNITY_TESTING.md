# Capen Major visual test

Open `C:/UBHackathon2026` through Unity Hub with **6000.6.7f1**. Let assets import,
open `Assets/Scenes/CapenMajor.unity`, and press **Play**. No setup command is needed.
The approved Capen artwork is already assigned to the existing scene's prefab.

## Controls

- Select **Laser** (selected initially), then hover over a recess to preview it.
- **Left-click** to place a free Laser; **right-click** to remove it.
- Hold **Q / E** to aim all guns, clamped to -60 / +60 degrees. **R** resets aim.
- Re-enter the scene to clear all placements. Other tower cards remain inspectable.

The same visual sandbox runs on Union Prime and Davis Nova. It does not spend
money/fuel or implement combat.

## Art review

Inspect all sixteen recesses empty and occupied, including side and upside-down
towers. Steel front edges should cover the foundations; only the guns should
rotate. Check the administration's curved stone foundation, station corners,
flag plaza contact, and both lower facades. These retain the approved assembly's
curves and transforms, with each facade in its own sprite layer.

Check 1280x720, 1672x941, 1280x960 and 2560x1080 for HUD clearance and pointer
placement. Return to LevelSelect and re-enter Capen Major; watch for Console errors.

## Rebuild and coordinates

Run `python Art/Planets/CapenMajor/prepare_unity.py` from the repository root.
Requires Pillow and Chrome; use `--chrome PATH` for another Chrome executable.
This exports the existing `planet-empty.svg` without repainting source artwork.
Do not run the general planet-page setup command to import these visuals.

Eight transparent, point-filtered sprites live in `Assets/Art/Planets/CapenMajor/`:
five independent facades, rear floors, terrain, and foreground steel edges.
The 1392x1420 export corresponds to viewBox `-80 -100 1392 1420`; mounts shift by
`(+80, +100)` into that canvas. Display width is 600 UI units and Laser source
scale is 1.5. Sockets are 75% of their previous size, at 22.5-degree intervals
clockwise from the top; body display size and facade placements are unchanged.
Existing scene/prefab GUIDs and shared Laser assets are preserved.

`unity-import.json` records hashes and export checks. Recombining the layers
matches the approved empty preview within 3/255 per visible color channel over
black and white backgrounds. Small differences come from alpha rounding.

## Sixteen-socket verification, 2026-11-07

The art rebuild passes 992 foundation samples and exact 22.5-degree radial
spacing. The closest adjacent mounts leave over 38 source pixels beyond the
126-pixel socket width. All sixteen sites were reviewed at -60, 0 and +60 degrees.
The eight exported layers match the empty preview within 3/255 per visible
channel. All five facade exports are byte-identical to the previous version;
source artwork hashes, sprite GUIDs and 600-unit display width are preserved.

Unity **6000.6.7f1**: **6 Play Mode tests passed, 0 failed**, running
`UnionPrimeVisualTestTests` across Capen Major, Union Prime and Davis Nova.
Capen placement/removal, duplicate rejection, aim bounds, fresh scene entry
and pointer interaction all exercise sixteen sites. Results are local at
`.git/menu-work/capen-sixteen-tests.xml` and `capen-sixteen-tests.log`.
No standalone build or interactive human review was run for this socket pass.

The follow-up upper-shoulder repair was checked in enlarged before/after renders
and refreshed empty/occupied previews, including the restored 10 o'clock panel
seam. All 992 foundation samples still pass;
socket coordinates and facade transforms are unchanged. Runtime layers were
re-exported. Play Mode tests were not rerun for this texture-only correction.

## Previous twelve-site verification, 2026-11-07

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
