# Davis Nova art handover

## Current design — 2026-11-07

The current design is the pixel-art mechanical ring and the new matching forge
sun. The user approved the broad, flat socket profile and requested the pixel
restyle, then replaced the supplied detailed sun with a simpler pixel design.
The flame-edged orange revision was rejected. The current sun is a much brighter
yellow-white disk with a narrow gold rim, no projecting flames, and no dark
planet-like surface patches. The ring and measured tower mounts are unchanged.
The sun is decorative; the ring holds twelve outward-facing tower sockets.

- [Full composition, empty and occupied](Pixel/page-review.png)
- [Top socket](Pixel/top-review.png)
- [All twelve contacts](Pixel/contact-review.png)
- [Unity testing instructions](UNITY_TESTING.md)

The sun, ring, and socket lips remain separate sprites. The lips are extracted
from the ring to overlap tower foundations; they are not extra decorative art.
There is a clear gap between the sun's round edge and the playable ring.

## Files to keep

- `ring-pixel-v1.png`: approved pixel ring source.
- `sun-pixel-source.png`: bright round forge sun, edited with built-in imagegen.
- `ring-pixel-v1-prompt.txt` and `sun-pixel-prompt.txt`: built-in imagegen prompts.
- `socket-layout.json`: measured pixel-ring contacts, source pivots and sun scale.
- `build_pixel_review.py`: independent SVG assembly and rendered validation.
- `Pixel/`: current PNG layers, review images and `assembly.json`.
- `prepare_unity.py` and `unity-import.json`: reproducible Unity asset preparation.

Old smooth designs, old reviews, obsolete builders, reference duplicates and
Python bytecode were removed at the user's request. Earlier committed designs
remain in Git history. The rejected supplied sun remains in the user's original
download at `C:/Users/matia/Downloads/davis_nova_sun.png`; it is not a dependency.
Generated SVG/Chrome scratch files live under `.git/menu-work/davis-contact-render`.

## Assembly and Unity

All three layers in `Pixel/` (`sun.png`, `ring-background.png`,
`socket-fronts.png`) use a 1254 x 1254 canvas centered at (627,627). Sun source
scale is 0.54; tower source scale is 2.0. The existing shared Laser base/operator
and gun remain independent, with their original pivots. Use this layout's mounts
with this ring; generated artwork is not an exact copy of the earlier geometry.

The source images have real alpha. SVG assembly removes near-zero alpha residue
and makes near-opaque metal/star pixels solid using slope 1.04, offset -0.02.
RGB is unchanged by this assembly step. Pixel steps in the diagonal lips vary by
up to 12 source pixels over the central 70-pixel sample.

`Assets/Art/Planets/DavisNova/` contains the three imported sprites. They use
point filtering, no mipmaps, no compression, full-rectangle meshes, centered
pivots and no generated physics outlines. The existing
`Assets/UI/Planets/DavisNovaVisual.prefab` retains its GUID and original Sun/Ring
object IDs. It displays the art at 600 UI units before the existing HUD scale.
Unity UI Images are used because that is the established planet-page renderer.

The prefab now uses Union Prime's free Laser placement component. `PlacedLasers`
replaces the inactive static `TowerFitPreview`: left-click a socket to place,
right-click to remove, Q/E to aim and R to reset. Only the guns rotate. The shared
component accepts a prepared tower layer and per-planet source size/scale; Union
Prime retains its existing defaults. No combat, resource spending, manager,
scene or project-setting changes are included. See `UNITY_TESTING.md`.

## Rebuild

From the project root with Python, Pillow and Chrome installed:

```powershell
$env:PYTHONPATH = Join-Path $PWD '.git/menu-work/python'
$env:PYTHONDONTWRITEBYTECODE = '1'
python Art/Planets/DavisNova/build_pixel_review.py
python Art/Planets/DavisNova/prepare_unity.py
```

The local `PYTHONPATH` is optional if Pillow is already installed. The preparation
script retains existing sprite GUIDs and is repeatable. Its target is only the
Davis Nova sprite folder and visual prefab. If artwork changes, remeasure the
contacts rather than assuming `socket-layout.json` still fits.

## Verification

Rendered checks pass: twelve sites at 30-degree intervals, 744 supported and
overlapped foundation samples, zero protected upper-base occlusion, and zero rim
occlusion over 23,004 gun samples at -60/0/+60 degrees. Sun corner alpha is zero,
center alpha is 255, and opaque sun pixels do not overlap the ring. The full
composition and all-site contacts were visually reviewed. See `UNITY_TESTING.md`
for Unity results and remaining manual checks.
