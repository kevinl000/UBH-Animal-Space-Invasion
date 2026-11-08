# Silverman Luna moon: preserved approved design

The user scrapped the Silverman Luna main planet on 2026-11-08 and explicitly
preserved this moon. Its approved source, four cardinal rock sockets, moon-only
renders and tower fit are unchanged. No main-planet artwork is needed to rebuild it.

## Current files

- `moon-small-source-v4.png`: unchanged approved generated source.
- `prompt-small-moon-v4.txt`: original generation prompt.
- `build_review.py`: independent moon assembly, rendering and verification.
- `FittedSites/terrain-background.png` and `socket-fronts.png`: separate sprite layers.
- [Empty and occupied moon](FittedSites/page-review.png).
- [Four sockets and three aiming poses](FittedSites/contact-review.png).
- [Transparent empty moon](FittedSites/moon-empty.png).
- [Transparent occupied moon](FittedSites/moon-occupied.png).
- [Assembly](FittedSites/assembly.json) and [verification](FittedSites/verification.json).

The obsolete `size-review.png` contained the scrapped main planet and was removed.
There are twelve current files in this moon folder.

## Approved size and assembly

Keep the moon canvas at **180 display units** for its 1254-pixel source canvas.
The tower's local scale remains **4.166667**, giving the shared 70-by-72-pixel
base a displayed size of 41.866 by 43.062 units. These values preserve the
approved smaller moon and tower size. The historical reference was a 600-unit
canvas with tower scale 1.25, making the moon canvas 30% of that reference;
it is now a fixed size rather than a dependency on a main-planet design.

The natural gray/ivory/slate rock and muted teal craters are retained. The
assembly reuses the source's top rock recess at all four cardinal sites with
rigid rotations and blended local joins. No tower is baked into the terrain.
Render background, towers, then socket fronts. Alpha below 240 is discarded
and a one-pixel morphological opening removes stray edge spurs.

## Rebuild and validation

From the project root, with Python/Pillow and Chrome installed:

```powershell
python Art/Planets/SilvermanLuna/Moon/build_review.py
```

Only the moon source and shared Laser base/gun sprites are input assets.
Scratch SVG/HTML/browser files are created under
`.git/menu-work/silverman-moon-render/`. Rebuilds do not recreate a main planet
or a combined main-planet comparison.

Offline checks cover all 248 foundation-column contacts, zero hidden upper-base
pixels and zero hidden gun pixels at -60, 0 and +60 degrees (7,668 gun samples).
The original approval also checked the same-scale moon-only renders. These are
offline artwork checks, not Unity gameplay or collision tests.

## Pending Unity integration

The approved moon remains a separate art assembly for independent orbit and
rotation. It has not been wired into a Unity prefab or scene. The existing
disabled generic moon placeholder is unrelated to this artwork. Orbit radius,
speed, rotation settings and final viewport remain unconfirmed.

Earlier moon drafts remain in the local `.git/menu-work/silverman-moon-drafts/`
archive. They are not rebuild dependencies.
