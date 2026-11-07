# Circular Union Prime terrain with natural grass

The next composition milestone is ready in [Composition/](../Composition/README.md):
accepted terrain plus unchanged building sprites, four placement adjustments and
separate guild supports. Review that assembled result before Unity integration.


**Approved checkpoint, 2026-11-04:** the user accepted this terrain and requested
cleanup of unused materials.

Current terrain: [terrain-circular.png](terrain-circular.png).
Review the [checker preview](terrain-preview.png) and
[circle overlay](circle-guide.png).

The uneven extracted contour is corrected to one circle centered at **(627, 610)**
with **radius 365** in the original 1254-square source coordinates. The user also
requested removing the bamboo/fence appearance introduced by the first generated
repair. The latest edge has short irregular green tufts, without a continuous
yellow rail or regular stalks crossing the twelve empty crater mouths.

## Files

- `terrain-body.png`: solid circular body, including the twelve dirt sites.
- `grass-fringe.png`: separate sparse exterior vegetation, at most 5 pixels beyond
  the circle. Composite it over the body at `(0, 0)` to obtain the current terrain.
- `base-circle-mask.png`: exact binary pixel-center circle mask, matching the body
  alpha. Bounds are `(262, 245, 992, 975)`, with right/bottom exclusive: 730 x 730.
- `changed-pixels-mask.png`: all changed pixels against the extracted terrain.
- `circular-terrain.json`: source hashes, geometry and verification results.
- `natural-edge-generated.png`: built-in imagegen texture repair used for the
  peripheral terrain; its approximate shape is constrained by the build script.
- `generation.prompt.md` and `generation-prompts.json`: edit provenance and exact
  prompts. Both edits used the built-in tool, with transparency, without CLI fallback.

## Rebuild and checks

With Python 3 and Pillow, run from the repository root:

```powershell
python Art/Planets/UnionPrime/Circular/build_circular_terrain.py
```

The output body alpha obeys this rule at every source pixel:

```text
(x + 0.5 - 627)^2 + (y + 0.5 - 610)^2 <= 365^2
```

The peripheral texture is sampled from the generated natural-edge repair with
nearest-neighbor mapping. Its source contour is measured to align it to the circle.
The original central **270-pixel radius** is preserved exactly (229,020 pixels).
A color transition over radii 270-315 avoids a visible join; old yellow grass pixels
are replaced directly so the transition does not restore stalks. Texture coordinates
are not spatially blurred. Exterior detail is restricted to opaque green vegetation.

Checks cover exact circle alpha, equal width/height, no body holes, binary final
alpha, twelve distinct large soil regions, unchanged center, source hashes, and
reproducible exports. The terrain and comparison were visually inspected for the
old stalk remnants and the transition seam. The obsolete comparison and first
bamboo-edge generated image were then removed; rebuilds do not recreate them.
The exact generation prompts remain historical provenance, including removed input
filenames. Only `natural-edge-generated.png` is needed by the current build script.

## Scope and next step

The body is circular; the dirt recesses remain artwork within it, with mouths open
at the surface. This does not standardize crater widths/depths or define gameplay
placement. The outer band is generated/remapped and is not pixel-identical to B-25.

B-25, `Working/`, and the source-preserving `Separation/` outputs remain unchanged.
No building sprite was edited or moved. The two historical image dependencies
`Working/planet-isolated.png` and `Separation/terrain-visible.png` remain solely
for reproducible exports; they are not alternative current terrain designs. Before assembling the final composition,
review facade/support contact against this corrected edge and complete hidden
surfaces where needed. Unity import, scene/prefab changes and Play Mode checks have
not been performed; the new terrain is a review artifact outside `Assets/`.
