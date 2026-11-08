# C3 buffet worlds — approved artwork and Unity visual

Three separate dining-hall-themed planets: **grill**, **salad bar**, and **bakery**.
Each has six outward-facing recessed tower seats, for **18 sockets total**.
The user approved the artwork on 2026-11-08 and requested Unity integration,
with the three planets a little closer. The existing `C3Visual.prefab` now
assembles the separate sprites with 18 free Laser placement sites.

Start with [the empty/occupied comparison](FittedSites/page-review.png),
[the full three-planet composition](FittedSites/c3-occupied.png), and
[all eighteen contact close-ups](FittedSites/contact-review.png).
Open [the motion preview](FittedSites/motion-preview.html) in a browser to see
three independent bodies orbit a common empty center and spin. Its controls
adjust orbit, spin, tower visibility and pause. These are visual settings,
separate from gameplay balance.

## Sources and exports

`grill-source-v1.png`, `salad-source-v1.png` and `bakery-source-v1.png` are the
intact transparent images generated with the built-in image-generation tool.
The matching `*.prompt.txt` files retain the complete prompts. Salad and bakery
are sibling theme edits of the grill source; Silverman Luna was used only as a
socket reference and is not a build dependency.

For each theme, `FittedSites/` contains:

- `*-terrain-background.png`: 1254×1254 transparent source-derived sprite.
- `*-socket-fronts.png`: matching canvas containing the measured painted lips.
- `*-empty.png` and `*-occupied.png`: padded 1536×1536 review renders.

The full compositions, comparison and contact sheets are review images, not
gameplay sprites. The tower artwork remains separate and unchanged; occupied
renders use `Art/Towers/Laser/Redesign/Sweep/base-sprite.png` and `gun-sprite.png`.

## Socket fitting and transforms

[assembly.json](FittedSites/assembly.json) records source hashes, six mounts per
planet, clockwise angles, local tangent offsets and the measured lip profiles.
Coordinates use the 1254-pixel source canvas, with origin at top left, x right,
y down. Rotation center is `(620,625)`. Tower scale is `3.125` in source pixels.
The 70×72 base uses foundation pivot `(34,71)`; gun pivot is `(12,28)` and the
base hinge is `(64,24)`. The gun rotates independently of its base.

Render **terrain → tower bases/guns → socket fronts**. The lower painted rim
covers the very bottom of the foundation, so towers sit inside the recesses.
Each of the eighteen existing painted recesses is measured independently;
there are no repeated perimeter patches or material seams. Tall side walls
remain behind the upper tower chassis. Terrain alpha below 240 is discarded
in the renderer to remove translucent generated fringe; source PNGs are intact.

For future Unity conversion, use point filtering, no mipmaps, uncompressed
sprites and matching full rectangular canvases for both layers. The intended
center `(620,625)` corresponds to a Unity normalized sprite pivot of approximately
`(0.494418,0.501595)`, after converting the y coordinate to bottom-origin.
Do not bake the three bodies, tower sprites or orbital motion into one sprite.

The combined review uses body scale `0.4`, orbit radius `376.2` review pixels and
120° phase separation. The second spacing adjustment brings centers **16.4%
closer than the first Unity version**, with the body and tower sizes unchanged.
The conservative occupied envelopes retain a **29.6-canvas-unit gap** before
page zoom, including the sampled gun poses.

## Unity integration

Run `python Art/Planets/C3/prepare_unity.py` after an artwork rebuild. It copies
six sprites to `Assets/Art/Planets/C3/`, preserves existing asset GUIDs and
updates only `Assets/UI/Planets/C3Visual.prefab`. It reuses the existing Laser
sprites and `UnionPrimeVisualTest` placement tool, without modifying that tool.
[unity-import.json](unity-import.json) records the imports and source hashes.

Each planet has a **240-unit source canvas**, with tower scale `3.125`. The
Laser base displays at **41.866 × 43.062 canvas units**, matching the approved
Silverman tower size. The orbital radius is **180 canvas units**. The
rectangles pivot around the measured source center so terrain, tower bases,
independently aimed guns, and foreground lips rotate together.

`Assets/Scripts/Menu/Planets/C3VisualMotion.cs` moves the three bodies 120° apart,
with **4°/s clockwise orbit** and **10°/s clockwise local spin**, carried over
from the approved browser preview. `Orbit Radius`, `Orbit Speed` and `Spin Speed`
are editable on the C3 visual prefab. Motion follows scaled time and stops
while the page blocks interaction during navigation/loading. No managers,
shared UI scripts, other planet prefabs or scenes were edited.

Open `Assets/Scenes/C3.unity` in Play Mode. Select Laser, click an empty socket
to place, right-click an occupied socket to remove, and use Q/E to aim or R to
reset aiming. Placement is free visual testing. Mouse-wheel/pinch page zoom
applies to all three worlds together. This does not create combat, resources,
enemy targeting or orbital physics.

## Rebuild and verification

With Python, Pillow and Google Chrome installed, run from the project root:

```powershell
python Art/Planets/C3/build_review.py
```

Chrome renders SVG sprite layers and masks. Pillow only reads pixels and sizes
for measurement and verification. Scratch SVGs, HTML and the browser profile
stay outside the artwork folder in `.git/menu-work/c3-render/`.

[verification.json](FittedSites/verification.json) reports rendered foundation
support and foreground contact, upper-chassis occlusion, gun clearance at
−60°, 0° and +60°, and a conservative separation check for the combined layout.
These are offline artwork checks. Continuous gun sweeps and combat collision
remain outside this visual integration.

`Assets/Tests/Menu/C3VisualTests.cs` covers independent placement/removal,
foundation and gun transforms, moving-pointer placement at minimum zoom,
orbital separation, pause behavior and fresh scene entry. Local Unity results
and rendered captures are under `.git/menu-work/c3-unity/`.

Before the second spacing adjustment, Unity **6000.6.7f1** verification passed
**12 Play Mode checks** on 2026-11-08:
the three C3 regressions, a render-capture check, all seven page zoom cases and
the destination-page availability/details check. Captures were inspected with
all 18 sockets occupied, ±60° aiming, maximum zoom, and a rotated cluster.
[unity-verification.json](unity-verification.json) records the results. The six
Unity exports match their reviewed source hashes, and rerunning the importer
preserves the prefab byte-for-byte. The second spacing adjustment was checked
offline; Play Mode was not rerun because the user's Unity Editor is open.
Reopen the C3 scene or restart Play Mode to see the 180-unit radius; check inward
facing occupied sockets while the three planets rotate.
The temporary capture test remains outside
Unity under `.git/menu-work/c3-unity/RenderCapture.cs`. No standalone build ran.
