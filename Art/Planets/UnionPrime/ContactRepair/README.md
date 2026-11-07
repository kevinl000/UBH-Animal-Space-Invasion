# Crater contact and terrain-join repair

2026-11-05, `planets-page`. Addresses the user's Unity screenshots of uneven
support under the guild/upper stalls and visible gaps along tower foundations.

## Artwork and layers

- Built-in imagegen edited the existing terrain. The unchanged output is
  [terrain-generated.png](terrain-generated.png), with the exact
  [prompt](generation.prompt.md). The preceding generated source remains intact.
- Packing adopts the perimeter repair while preserving the previous central
  radius-300 ocean/continent pixels. The ground behind facades is continuous;
  soil highlights are quieter and the outer grass ledge is connected.
- Foreground masks follow each painted soil edge, with ten source pixels of
  soil above its inward boundary and the terrain immediately inward of it.
  The outer seven pixels of the nominal radius-365 support stay behind facades.
- Guild support/foundation masks contained old terrain. Those pixels are removed
  inside the repaired ground, with two pixels of overlap retained at its edge.
  Facade dimensions/origins, building artwork and other facade sprites stay fixed.
- Back-to-front layers remain rear ground, facades, terrain, Laser base/operator,
  aiming gun, foreground. No runtime script, manager or scene changed.

## Seating

Crater locations and angles stay fixed at their radius-330 reference. Every Laser
keeps its 150% scale. Because the painted floors have different depths, tower
mounts move inward 3-15 source pixels (about 2-9 pixels at the reference page's
display scale), derived from the lower foundation contour rather than guessed
independently. Each site's old crater reference, inset and final mount are stored
in [polish.json](../polish.json). The Unity prefab imports those final mounts.

This preserves room around the Laser; it does not shrink craters to fit one tower
or establish placement rules for unspecified future towers.

## Verification and limits

- All 96 sampled lower-foundation columns per site touch the foreground in the
  assembled source coordinates, including rotated/rounded mount positions.
- All 2,892 site/angle samples pass gun/terrain, thin muzzle-ray/terrain and
  gun/foreground checks. Base overlap is now 713-983 pixels, reflecting seating.
- Inspected [empty/occupied composition](../page-review.png),
  [orientation closeups](../orientation-closeups.png) and
  [all twelve sites at three aim angles](../all-sites-aiming.png).
- Offline compilation passes for Managers, Menu, Menu.Tests and menu editor,
  with only existing AudioManager warnings. Log:
  `.git/menu-work/contact-repair-compile.log`.
- Repeat build reproduced all 73 checked files. All nineteen Unity sprite hashes
  match their source files, metadata matches HEAD allowing checkout line endings,
  and all twelve prefab mounts match the manifest. Central RGB preservation and
  `git diff --check` pass. Audit: `.git/menu-work/contact-repair-verification.json`.
- Attempted Unity 6000.6.7f1 Play Mode tests, but Editor licensing failed before
  project load (exit 198, no valid Editor license). Log:
  `.git/menu-work/contact-repair-unity.log`. No Unity import, Play Mode test,
  standalone build or actual Game view resolution check passed in this session.

Open `Assets/Scenes/UnionPrime.unity` through Hub, press Play and review empty and
occupied sites at 1280x720, 1672x941, 1280x960 and 2560x1080. Check the guild and
upper sides, side/lower tower contacts, click placement/removal, Q/E aiming limits
and R reset. Run `UnionPrimeVisualTestTests` and `PlanetPageTests` in the Play Mode
Test Runner. Visual acceptance remains with the user.

Rebuild from the project root using Python/Pillow:

```powershell
python Art/Planets/UnionPrime/Facades/extract_facades.py
python Art/Planets/UnionPrime/build_polish.py
python Art/Planets/UnionPrime/prepare_unity.py
```

On this workstation Pillow is in `.git/menu-work/python`; set `PYTHONPATH` to that
directory first. Rebuilds use saved artwork and make no new generation requests.
Unity sprite GUIDs are preserved. Changes are uncommitted; no push.
