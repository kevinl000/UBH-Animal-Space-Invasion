# Davis Nova art test

Open `C:/UBHackathon2026` through Unity Hub with **6000.6.7f1**. Let the assets
import, open `Assets/Scenes/DavisNova.unity`, and press **Play**. No setup menu
command is needed. The existing scene uses the updated `DavisNovaVisual` prefab.

## Empty ring and sun

Check the bright round pixel sun inside the ring, transparent space between its edge
and the inner rim, twelve broad empty seats, and the HUD. Both sprites are
separate children of `DavisNovaVisual`; the sun is behind the ring.

## Tower fit

In Play Mode, find `DavisNovaVisual` in the Hierarchy and enable its inactive
`TowerFitPreview` child using the checkbox at the top of the Inspector. Twelve
static Lasers should appear with their foundations behind `SocketFronts`.

- Check the upper, side and upside-down seats for contact or obscured characters.
- Expand a `SiteXX` and select `AimPivot`. Set its local Z rotation to -60, 0
  and +60 degrees to inspect the independent gun. Do not rotate `LaserBase`.
- Disable `TowerFitPreview` to return to the empty-ring view. Play Mode changes
  are temporary; stop Play Mode to reset them.
- Review 1280x720, 1672x941, 1280x960 and 2560x1080 for size and HUD clearance.
- Return to LevelSelect and re-enter Davis Nova. Check for missing sprites and
  Console errors.

This is a static art test. Clicking a socket does not place a tower; tower cards
retain the existing information-only behavior. No combat or resource logic was added.

## Automated verification

The current art passes the rendered contact/transparency checks recorded in
`Pixel/assembly.json`. Unity preparation is repeatable and preserves asset GUIDs.

The bright round sun revision passes the rendered checks; its Unity sprite is
updated with the same GUID, dimensions and import settings. Unity Play Mode has
not been rerun for this texture-only revision.

For the preceding flame-edged sun revision, Unity **6000.6.7f1** imported the
sprites and prefab successfully. The existing Play Mode test
`PlanetPageTests.EveryDirectEntryShowsCorrectAvailabilityAndInspectableDetails`
passed: **1 passed, 0 failed**. That earlier result's local path is
`.git/menu-work/davis-nova-art-tests-final.xml` and its log is
`.git/menu-work/davis-nova-art-unity-final.log`.

The automated scene-entry check covers the existing HUD and planet pages; it
does not replace the manual art, aiming and resolution checks above. No
standalone build or interactive Game-view review has been performed.
