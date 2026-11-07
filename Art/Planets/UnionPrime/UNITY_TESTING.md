# Union Prime Laser visual test

Open `C:/UBHackathon2026` through Unity Hub using **6000.6.7f1**. Let assets import
and scripts compile, open `Assets/Scenes/UnionPrime.unity`, then press **Play**.
No setup menu command is needed. The detailed art is assembled in Play Mode;
the original silhouette remains visible in edit mode.

## Controls

- Select **Laser**, the first roster card (selected initially).
- Hover an empty crater for a translucent preview; **left-click** to place.
- **Right-click** a crater to remove its Laser.
- Hold **Q / E** to aim every placed gun together, up to 60 degrees either way.
- Press **R** to reset the guns to their local outward direction.

Each of the twelve sites holds one Laser. Placement/removal is unlimited and costs
no money or fuel. Other cards can be inspected but cannot be built. Resource/stat
readouts are presentation values. Leaving and re-entering the scene clears placements.
This is an art sandbox: guns do not target or fire at enemies.

## Review in Game view

1. Fill all twelve sites, remove several, then rebuild them. Check that empty craters
   accept clicks and occupied ones do not stack towers. Planet-center and UI clicks
   should not build anything.
2. Inspect upper, side and upside-down towers at neutral and both aiming extremes.
   Check foot/floor contact, foreground soil overlap, gun/socket contact, rabbit
   readability and overlap with the rear buildings.
3. Compare 1280x720, 1672x941, 1280x960 and 2560x1080. Check that HUD controls remain
   readable/clickable and placement previews line up with the visible craters.
4. Return to LevelSelect and re-enter Union Prime: sites should be empty. Other
   planet pages should retain their original presentation.

The current [terrain/background polish](README.md) adds rear
supporting ground, repairs the lower doubled rims and texture joins, and uses
quieter facades with recognizable merchandise. All twelve mounts and the 150%
Laser remain. Review the guild threshold and lower/side craters both empty and
filled, and judge the revised facade contrast at actual Game view size.

## Verification status

**Current polish, 2026-11-05:** offline compilation of Managers, Menu, Menu.Tests
and menu editor scripts passed; only the existing AudioManager warnings remain.
All 2,892 sampled site/angle clearance checks passed, with unchanged base overlap.
Existing GUIDs, source masks and site transforms are retained. No Unity import or
Play Mode run has been performed for this artwork revision. Local compile log:
`.git/menu-work/terrain-polish-compile.log`.

The following records describe the previous sandbox revision:

Offline compilation passed for Managers, Menu, Menu.Tests and editor scripts using
the installed Unity libraries. Source-image hashes, sprite settings and new prefab
references were checked. The only compile warnings were the existing serialized
AudioManager fields. Local log: `.git/menu-work/union-prime-visual-test-compile.log`.

**User verification, 2026-11-05:** the user opened the sandbox in Unity and confirmed
"Ok it works", with a Game view screenshot. They requested terrain/crater polish
and subsequent background-structure improvements; see the next-session brief in
`PLANETS_PAGE_PLAN.md`. This confirms the sandbox workflow, not final art approval
or full resolution coverage. After the small code cleanup, offline compilation
passed again; no additional Unity run was performed by the agent.

**Automated Play Mode results and a standalone build remain unreported.**
In **Window > General > Test Runner > PlayMode**,
run `UnionPrimeVisualTestTests` (two tests) and the existing `PlanetPageTests`.
The new tests cover placement/removal at all twelve sites, duplicates, selection,
aim bounds, scene reset, pointer positions and UI input isolation. They compile;
their runtime results remain pending.

The user handles Unity checks through Hub. Record future art-review findings and
test results separately in `PLANETS_PAGE_PLAN.md`.
