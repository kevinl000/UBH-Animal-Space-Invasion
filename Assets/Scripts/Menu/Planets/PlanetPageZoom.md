# Planet and gameplay zoom

The existing PlanetPageZoom controller handles both world-space gameplay and
older overlay planet pages. Mouse wheel up / two-finger spread zooms in; wheel
down / pinch zooms out. Defaults remain 0.5-1.0, starting at 0.75, with unscaled
smoothing and the existing wheel sensitivity.

Union Prime receives the controller from GameplayBootstrap. GameplayHUD passes
its fitted camera size to the controller, including after a viewport resize.
Camera zoom scales the planet, towers, enemies, projectiles, effects and socket
ghosts together. The overlay HUD stays fixed. World physics, ranges, movement,
spawn distances and parent hierarchies do not change.

The older destination pages retain the controller on PlanetHUD.prefab.
Their PlanetVisual art still uses its authored scale multiplied by zoom. In a
gameplay scene, the camera changes by the inverse zoom factor around the same
planet centre. This gives world-space combat sprites the same visible scale as
the overlay planet, while keeping its measured world radius and centre stable.
Pages without LevelPlanetDefense retain their original art-only zoom.

The fitted view is the starting zoom. Camera size is fitted size multiplied by
startingZoom / CurrentZoom. An aspect-ratio change preserves CurrentZoom.
Zoom is blocked during pause, loading, disabled page controls, results and
Union Prime's boss entrance. The entrance owns the camera until it returns to
the saved position and chosen zoom.

Input must be inside the battle camera viewport (world-space gameplay) or
ZoomViewport (overlay pages). HUD buttons and scrolling controls reject zoom.
The wheel over the roster scrolls the roster. Both pinch fingers must start
and remain in the playfield. Stars authored on the overlay HUD stay fixed.

## Existing overlay spawn helper

MaximumViewBounds and GetSpawnPosition remain available for overlay visual
authoring. They return PlanetVisual local coordinates at minimum zoom. Current
runtime EnemySpawner uses its configured world-space planet-radii distances;
it does not use this historical overlay spawn helper.

## Manual verification

Do not launch Unity or run Unity test suites through Codex. Team review:

1. Open Union Prime directly and through navigation. Wheel and pinch to both
   limits in setup and combat. Verify planet, enemies, towers, shots, effects
   and ghosts change size together while HUD, costs and combat behavior remain
   consistent. Place towers at both limits, including by numbered socket.
2. Scroll the roster and hover the corner controls / bottom bar. These must
   not zoom the battle. Check pause, loading, results and the boss entrance;
   the camera must return to the chosen zoom after the entrance.
3. Resize between 16:9 and 4:3 while zoomed. Verify the chosen zoom is retained,
   the right sidebar and bottom row stay fixed, and placement still aligns.
4. Open an older gameplay planet. Verify its overlay art and combat sprites
   zoom together with no drifting, changed orbit radius or tower seating.
   Check Silverman's moon and its towers separately.
