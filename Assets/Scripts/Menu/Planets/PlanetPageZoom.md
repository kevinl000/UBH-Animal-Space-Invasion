# Planet and gameplay zoom

The existing PlanetPageZoom controller handles both world-space gameplay and
older overlay planet pages. Mouse wheel up / two-finger spread zooms in; wheel
down / pinch zooms out. Limits are 0.625-0.875, starting at 0.75: half the previous
travel in each direction. Smoothing and wheel sensitivity are unchanged.

Union Prime receives the controller from GameplayBootstrap. GameplayHUD passes
its fitted camera size to the controller, including after a viewport resize.
Camera zoom scales the planet, towers, enemies, projectiles, effects and socket
ghosts together. The overlay HUD stays fixed. World physics, ranges, movement
speeds and parent hierarchies do not change.

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

## Incoming enemy border

EnemySpawner uses TryGetSpawnDistance to intersect the director's chosen angle
with the playfield rectangle at maximum zoom-out. Union Prime uses its fitted
camera viewport, excluding the right roster and bottom build bar. Older overlay
planets use spawnViewport, projected into gameplay space at minimum zoom.
Changing the player's zoom does not move the entry border. Resizing the viewport
recalculates it for new spawns; existing enemies keep their positions.

BaseEnemyData includes the scaled ship/shield bounds as clearance, then resets
the pooled body's interpolation at its entry position. Regular enemies,
reinforcements and encounters enter from offscreen. Summoned/released minions
keep their parent-relative distance. Scenes without a gameplay zoom camera
retain the configured planet-radii spawnDistance fallback.

## Existing overlay art helper

MaximumViewBounds and GetSpawnPosition remain available for overlay visual
authoring. They return PlanetVisual local coordinates at minimum zoom; runtime
spawning uses the world-space distance helper above.

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
   At maximum zoom-out, check incoming ships enter through all four edges with
   no popup, including after pool reuse and when reinforcements resume. Zoom in
   and back out while ships approach; their positions must not jump. Confirm
   summoned minions still emerge beside their parent ship.
4. Open an older gameplay planet. Verify its overlay art and combat sprites
   zoom together with no drifting, changed orbit radius or tower seating.
   Check Silverman's moon and its towers separately.
