# Union Prime: art sources and previews

This is the art-production workspace for the accepted Union Prime artwork.
Edit and rebuild artwork here, then export it to Unity.

| Location | Purpose |
| --- | --- |
| `Art/Planets/UnionPrime/` (this folder) | Source images, prompts, masks, layouts, rebuild scripts and review compositions. |
| [Assets/Art/Planets/UnionPrime/](../../../Assets/Art/Planets/UnionPrime/README.md) | Nineteen exported game sprites with Unity import settings and stable GUIDs. |
| [UnionPrimeVisual.prefab](../../../Assets/UI/Planets/UnionPrimeVisual.prefab) | Assembles the game sprites and measured tower mounts. |

The matching PNGs in the two art folders are intentional source/export copies.
Unity uses the copies under `Assets`; the top-level `Art` folder stays outside
Unity's asset import pipeline. Full compositions such as `planet-empty.png`,
`planet-occupied.png` and the review sheets are previews; the game assembles
separate layers instead.

Make artwork changes through the source workflow below. Direct pixel edits to
the exported game sprites will be overwritten by `prepare_unity.py`. Keep Unity
import settings in their existing `.meta` files and preserve those files when
moving sprites. [unity-import.json](unity-import.json) records the exact
source-to-export mapping and SHA-256 hashes.

## User-selected crater references

![Empty and occupied Union Prime](page-review.png)

![All twelve sites at three aiming angles](all-sites-aiming.png)

These exact images are the primary targets for Capen's terrain/tower relationship.
The specifically reused [top-site contact example](References/crater-contact.png)
and [transparent soil cross-section](References/crater-cross-section.png)
are retained as reference inputs, not an alternative prototype pipeline.

## What remains

- This directory: final generated terrain sources, exported layers, reviews,
  source hashes/contact checks and current rebuild/import tools.
- `Facades/`: final facade layers and the generated sources for
  the accepted facade, bakery and lower-arc orientation treatments.
- `Sources/`: thirteen live facade alpha masks, their origins,
  and the initial site layout used by the contact-fitting rebuild.
- `References/`: the two original crater images explicitly reused
  by the user and the Capen workflow.

## Cleanup, 2026-11-06

Removed the obsolete `Working`, `Circular`, `Composition`, `Extraction`,
`Separation` and `CraterPrototype` stages. Only data still required by the current
pipeline was retained in `Sources`; old colored mask-source sprites, exploratory
generations, guides, comparisons and obsolete build scripts were removed.
Git history preserves the deleted experiments; they are not another live archive.

The final facade/terrain rebuild reproduces all 27 existing final artwork PNGs
byte-for-byte. Nineteen Unity imports match their retained source hashes, and
the existing runtime asset/metadata files remain unchanged. The rebuilt twelve
sites pass the existing 2,892 sampled clearance combinations. This cleanup
does not claim a new Unity Play Mode or standalone-build test.

## Final terrain and facade pipeline

The user selected this Union Prime design. Keep the full-color guild/market
facades, repaired terrain contact, lower-right bakery and radial orientation of
lower-arc goods. The discarded background tint/dimming experiment is not part
of this artwork.

- [Empty / occupied composition](page-review.png).
- [All twelve sites at three aiming angles](all-sites-aiming.png).
- [Orientation closeups](orientation-closeups.png).
- [Final mounts, contact checks and hashes](polish.json).
- [Unity source/import mapping](unity-import.json).
- [Unity controls and manual verification](UNITY_TESTING.md).

## Current inputs and outputs

`terrain-generated.png` supplies the preserved planet center;
[ContactRepair/terrain-generated.png](ContactRepair/terrain-generated.png)
supplies the repaired perimeter. `Sources/site-layout.json` supplies the initial
site layout; `build_polish.py` measures the painted soil and computes contact
insets. Use the resulting `polish.json` for runtime mounts.

`Facades/facades-generated.png`, the bakery generation and the lower-arc
orientation generation supply the facade pixels. Thirteen alpha masks and their
origins live in [Sources](Sources/README.md). `Facades/extract_facades.py` produces
thirteen independent sprites and `Facades/facades.json`.

Back to front: `rear-ground.png`, thirteen facade sprites, `terrain.png`, the
separate Laser base/operator and gun, then `foreground.png`. There are twelve
sites with the accepted 150% Laser. The reference radius is 330; measured
contact insets determine the final mounts. Rear ground and facades are decorative
and do not participate in terrain clearance.

All current generation prompts remain next to the source images. The original
[crater contact example](References/crater-contact.png) and
[soil cross-section](References/crater-cross-section.png) remain available for
Capen. Unused prototype stages have been removed; see the cleanup notes above.

## Rebuild

From the repository root with Python and Pillow:

```powershell
python Art/Planets/UnionPrime/Facades/extract_facades.py
python Art/Planets/UnionPrime/build_polish.py
# Only when intentionally updating Unity imports:
python Art/Planets/UnionPrime/prepare_unity.py
```

The first two commands rebuild source exports and reviews from retained inputs.
The final command copies nineteen sprites and prepares the existing Union Prime
prefab, preserving existing GUIDs. It is not needed to open the current scene.

## Cleanup verification, 2026-11-06

After consolidating dependencies, the first two commands reproduce all 27 existing
final artwork PNGs byte-for-byte. All nineteen Unity sprite imports match their
source hashes. Existing Unity asset files, metadata and prefab are unchanged.
The current build passes 2,892 sampled gun/terrain, gun/foreground and thin
muzzle-ray checks, along with all twelve sites' foundation and facade contacts.
This verifies the cleanup preserves the accepted art; it is not a new Unity
Play Mode run or a projectile-width guarantee.

Earlier development and user-run Unity checks are documented in the testing
guide and root blueprint. Automated Play Mode coverage, standalone builds and
future tower fitting remain separate from this filesystem cleanup.
