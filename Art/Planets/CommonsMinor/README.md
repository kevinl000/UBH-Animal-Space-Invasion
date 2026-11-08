# Commons Minor artwork

Current candidate: `commons-source-v6.png`. The user's latest direction on
2026-11-08 retains V4's softer curved grass shoulders while widening and
flattening the middle of each recess for the tower foundation. Thin continuous
pale earth rims, the round grassy body, Commons clock tower and two restaurants
remain. V5's longer straight sockets are superseded. This combined revision
still needs user visual review.

- [Empty and occupied composition](FittedSites/page-review.png)
- [Eight socket contacts at three aiming angles](FittedSites/contact-review.png)
- [Transparent empty sprite](FittedSites/planet-empty.png)
- [Transparent occupied preview](FittedSites/planet-occupied.png)
- [Measured assembly](FittedSites/assembly.json)
- [Offline verification](FittedSites/verification.json)

All six sources were generated/edited with the built-in imagegen tool. Prompts
are saved as `generation.prompt.txt`, `socket-correction.prompt.txt`,
`spherical-body.prompt.txt`, `shallow-sockets.prompt.txt` and
`flat-floor-refinement.prompt.txt` and `soft-sockets-flat-middle.prompt.txt`.
V1 through V5 remain preserved earlier drafts; V6 is the only current build input. The occupied PNG is a review
composite, not the terrain sprite; no defense towers are baked into the
imported terrain.

## Socket assembly

The 1254-square sprite canvas displays at 428.571429 UI units. The existing Laser base
and gun are reused unchanged at source scale 1.75, making the 70-by-72 base
41.866 by 43.062 UI units before the planet page's zoom. This matches the
approved moon/reference tower size. Eight outward-facing mounts are individually
fitted to the generated earth contours, rather than assuming an exact circle.
The two upper diagonal floors use 36 and 324 degree orientations to follow
their drawn tangent; the other six retain their cardinal/diagonal orientations.
No gameplay radius, gravity or balance values are assigned.

The larger source-space tower fits the wider floors; the reduced planet canvas
keeps the displayed towers the same size as before and gives this eight-slot
planet its small-medium presentation.

Render terrain, placed towers, then socket fronts. The front masks follow the
pale earth lip and are limited to the foundation band; rear earth walls stay
behind operators. The shared base pivot is (34,71), the weapon hinge in base
coordinates is (64,24), and the gun pivot is (12,28), in image coordinates.

Offline rendered checks passed 496 supported and overlapped foundation-column
samples, with zero covered upper-base samples and zero covered gun samples
across 15,336 gun samples at -60/0/+60 degrees. The composition and contact sheet
were visually inspected. These are artwork checks, not combat/collision tests.

On the earlier V3 revision, Unity 6000.6.7f1 batch verification was attempted with the existing
`PlanetPageTests.EveryDirectEntryShowsCorrectAvailabilityAndInspectableDetails`
test. Startup spent several minutes indexing packages and then stopped producing
import progress before tests began. The dedicated batch process was stopped to
release the project. No Play Mode result was produced or claimed. Its log is
`.git/menu-work/commons-minor-unity.log`. No further Unity run was started for
V6; manual Play Mode verification remains pending.

## Unity preparation and rebuild

From the project root, with Python/Pillow and Chrome installed:

```powershell
$env:PYTHONDONTWRITEBYTECODE = '1'
python Art/Planets/CommonsMinor/build_review.py
python Art/Planets/CommonsMinor/prepare_unity.py
```

Scratch browser/SVG files go under `.git/menu-work/commons-minor-render/`.
The importer updates only `Assets/Art/Planets/CommonsMinor/` and the existing
`Assets/UI/Planets/CommonsMinorVisual.prefab`, preserving existing GUIDs and
root/body object IDs. Sprites use point filtering, no mipmaps, no compression,
full-rectangle meshes, centered pivots and no generated physics shape.
Unity UI Images follow the established planet-page renderer.

The existing shared placement component provides the free Laser visual sandbox:
select Laser, left-click a socket to place, right-click to remove, Q/E to aim,
R to reset. The imported foreground remains above the tower layer. No manager,
shared script, scene, C3 or Silverman Luna changes are part of this artwork.

Manual Unity review: open `Assets/Scenes/CommonsMinor.unity` in Unity
6000.6.7f1, enter Play Mode, inspect at normal and enlarged zoom, place/remove
Lasers at all eight sites and aim both ways. Check the upper curve, clock roof,
restaurants and tower clearance against the surrounding HUD. Final in-editor
appearance still needs a visual review; the offline previews use the same
mounts, pivots and sprite layers as the prefab.
