# Top crater prototype with the selected 150% Laser

2026-11-05. The user approved proceeding with a top-site prototype: flatter
tangential floor, short soil banks, and a separate irregular foreground edge.
The user then accepted the result ("Ok that works"). This is the approved
top-site reference; fit at the other eleven sites remains to be checked.

The subsequent [twelve-site fit review](FitReview/README.md) is now available.
All sampled aiming checks pass; it documents old rim remnants, texture joins
and exposed rear gaps that need visual finishing before production.

- [Before/after and empty/occupied closeups](crater-comparison.png).
- [Full planet comparison at offline page scale](page-review.png).
- [Both aiming limits and neutral](aiming-review.png).
- [Placement, input hashes, edit bounds and checks](prototype.json).

Only site 12 changes in the prototype. The approved Circular terrain,
Composition images, tower source sprites and previous study images are intact.
The other eleven sites remain empty and unchanged.

## Shape and layers

The floor now cuts into the planet silhouette, with short sloping banks and an
irregular nearly horizontal soil edge. The 150% tower mount is `(627,280)` in
the 1254-square source canvas, 13 pixels inward from the previous `(627,267)`
baseline. This mount is accepted for the top-site reference; it is not yet a
verified rule for all sites.

The comparison includes the old crater at the same deeper mount to distinguish
the geometry/layer change from seating depth alone. The empty/occupied versions
share exactly the same terrain; occupation only adds the separate tower and
foreground soil layer.

Back-to-front assembly:

1. Original guild support and facade sprites at their existing positions.
2. [Prototype terrain](terrain-prototype.png).
3. Unchanged Laser base and gun, uniformly transformed to 150%.
4. [Separate foreground soil](foreground-soil.png), using the actual irregular
   floor contour. Its rectangular extraction boundary lies below the visible
   soil edge; it does not create the straight cutoff from the prior diagnostic.

[site-edit-mask.png](site-edit-mask.png) defines the local replacement selection.
It includes the old dirt patch's grass perimeter to avoid a leftover second rim.
Outside that mask, terrain pixels are identical to the approved terrain.
The central notch is genuinely transparent, so the prototype intentionally
changes the old exact-circle alpha at this one site.

## Review findings and limits

The occupied view meets the foot to the floor and occludes 587 opaque base
pixels, leaving the operator, bracket and gun visible. The new empty recess
reveals a gap below the guild threshold because the extracted facade has no
hidden foundation there. That is visible in the empty closeup and is not fixed
by this prototype. A rear support/back-wall treatment would need a scoped
follow-up before production; no building artwork was modified to conceal it.

The floor leaves room beside the compact mounting foot, but this is not proof
that the full future tower roster fits. Side, lower and crowded-site versions
remain pending review. The generated soil texture is downsampled with nearest
filtering; final pixel density/palette finishing and Unity filtering remain open.

Verified 241 half-degree samples from -60 through +60 degrees: no gun or thin
muzzle-ray overlap with the prototype terrain, and no gun occlusion by the
foreground layer. Original assembly reconstruction is pixel-identical to the
approved composite. The rebuild checks unchanged reference hashes and no
terrain changes outside the site mask. These are offline raster samples, not
continuous collision proofs, projectile-width validation, or Unity Play Mode.
Two consecutive builds produced identical exported image and metadata hashes.

## Generation and rebuild

Built-in imagegen edited [terrain-reference.png](terrain-reference.png), using
[mount-reference.png](mount-reference.png) only for the tower's proportions.
The full prompt is [generation.prompt.md](generation.prompt.md). Its output is
retained unchanged as [crater-generated.png](crater-generated.png); no CLI/API
fallback was used. Reference terrain crop is `(499,205,755,365)`, enlarged 4x.
The mount reference is a 3x crop of `(499,125,755,365)` with the tower at `(627,283)`.
The final prototype uses the documented `(627,280)` mount to meet the generated
floor edge.

`build_prototype.py` selects the generated patch, removes alpha below 128 from
the packed copy's fringe while retaining the untouched generated source, and
reassembles the original facades behind it. No tower or building pixels are
generated or repainted. It exports transparent full-resolution empty/occupied
composites, the separate foreground layer, review sheets and verification data.

From the project root with Pillow installed:

```powershell
python Art/Planets/UnionPrime/CraterPrototype/build_prototype.py
```

On this workstation, set `$env:PYTHONPATH = 'C:/UBHackathon2026/.git/menu-work/python'`
first. The builder imports the adjacent `study_transforms.py`; the superseded
ScaleReview experiments were removed after the user approved this design. Everything stays
outside `Assets/`; no Unity imports, scenes, managers or gameplay changed.
