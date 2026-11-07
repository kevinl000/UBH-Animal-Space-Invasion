# Planet page zoom

All seven destination scenes inherit `PlanetPageZoom` from `Assets/UI/Planets/PlanetHUD.prefab`.
Select the prefab root to tune **Minimum Zoom**, **Maximum Zoom**, **Starting Zoom**,
**Wheel Sensitivity**, and **Zoom Speed**. Zoom values multiply the authored `PlanetVisual`
scale: defaults are 0.5 to 1.0, starting at 0.75. The original artwork is unchanged.

- Mouse wheel up enlarges the planet; wheel down reveals more space.
- Spread two fingers to enlarge; pinch them together to zoom out. Both fingers must
  begin inside `PlayArea/ZoomViewport`. A single finger still operates the tower roster.
- Zoom stays centered on the planet. Stars, HUD, and tower roster stay fixed.
- The wheel over the roster scrolls the roster. Scene loading and disabled page
  controls block zoom. Wheel smoothing uses unscaled time.

These scenes currently use screen-space UI images, including the placed test towers.
Scaling their shared parent preserves the existing layer order and hit testing. An
orthographic camera's size would not affect this overlay canvas. This is a presentation
transform; do not use the scaled canvas distances for combat ranges, speeds, or physics.
If gameplay is moved to world-space sprites and physics, use orthographic camera zoom
for that world rather than scaling physics objects.

## Spawn integration

`ZoomViewport` marks the usable playfield: 20 canvas units from the left edge, 20
from the 440-unit sidebar, 120 below the top, and 100 above the bottom. It stretches
with the canvas. It is a coordinate/input rectangle, not a visible border or mask.
`Spawn Viewport` references that rectangle by default.

`MaximumViewBounds` returns this rectangle in **PlanetVisual local coordinates**
at minimum zoom. It stays unchanged when the player zooms and recalculates when the
viewport size/aspect changes. Do not spawn relative to the current zoomed-in view.

`GetSpawnPosition(angleDegrees, clearance)` returns a local point just outside that
rectangle. Angles run counterclockwise: 0 = right, 90 = up. Pass the enemy's bounding
radius plus any extra gap as `clearance`, in PlanetVisual local units. One extra unit
keeps even a zero-radius point outside the border.

For a future UI enemy visual, parent it to `page.visualMount` (or an enemy layer with
the same origin and unit scale), anchor it at the center, and assign the returned
point to `anchoredPosition`. Keep simulation positions and distances in these unzoomed
local units. Clip enemy visuals to the usable playfield when wiring that renderer so
they do not appear over the HUD. Do not parent enemies to a rotating planet art child.

There is no live enemy spawner in these scenes yet. This change supplies the boundary
and position calculation; it does not change the wave director or create enemy waves.

## Verification

Run `PlanetPageZoomTests`, `PlanetPageTests`, and `UnionPrimeVisualTestTests` in Unity's
Play Mode Test Runner. On a touchscreen device, also check pinch direction, clamp feel,
lifting/replacing fingers, and tower-roster dragging in the intended orientation.
