# Davis Nova gameplay

This is level 2, after Capen Major. It originated on `feature/davis-nova-gameplay`
in the isolated `ubh-davis-nova` worktree. On 2026-11-08 the Davis-specific scene,
configuration and synchronized supernova were installed into the user's open
`C:/UBH-Animal-Space-Invasion` checkout for this sizing/blast repair. It uses the shared gameplay loop, managers,
adaptive director, pools, placement, speed/zoom controls and results. Capen and
Union scenes, shared unit data, wrappers, approved art and import GUIDs are not
changed by this implementation.

## Sun blast: confirmed replacement for per-tower lifespans

Every **40 combat seconds**, the sun destroys **all currently living towers**
in this scene by removing their remaining HP through the normal death/pool
path. This includes starter, frozen, unpowered and newly built towers. Tower
age and fuel do not change the deadline. No refund is awarded; sockets become
available for paid replacements. Enemy HP, planet HP, resources and existing
projectiles are unaffected by the blast itself.

The single countdown starts at 40 when combat begins, continues across
Wave/Clear/BossFight and carries its remainder across safe breaks. Setup,
breaks, boss entrances, pause, loading and results hold the clock; 2x speed
advances it twice as quickly in real time. The sun pulses during the final
five seconds. A dedicated eight-frame pixel supernova begins ignition during
the final 0.3 seconds, reaches its full bright disk at zero, then dissipates into
broken shockwave arcs. All twelve occupied sockets are snapshotted and destroyed
in that single zero-countdown frame, without waiting for the visual to travel.
The old copied-sun pulse and per-tower explosion effect are no longer wired here.
Shared explosion assets remain available to their other consumers.
The countdown states that all towers will be destroyed. Building late in a
cycle does not grant immunity. No per-tower `TowerLifespan` remains in Davis.
Retry reloads the scene and resets the cycle.

`DavisNovaSunBlast` owns this rule and its noninteractive countdown. The
scene component exposes the interval and warning time for tuning. The old
`TowerLifespan` script is preserved for legacy references and is not edited.

## Initial level 2 tuning

Start with **75 HP / 10 money / 50 fuel**, two free opposite Lasers, twelve
sockets, clockwise rotation at six degrees/second. Laser, Bomb, Sniper and
Pest Trap carry over; **Laser SMG** unlocks here at its existing cost/stats.
All three spawn windows last 20 seconds; waves 1 and 2 clear completely before
their 10-second safe breaks. Build in combat; sell for full refunds only in
setup/breaks. No passive repair, blast compensation or free rebuild is added.

| Wave | Budget | Cost shares | End encounter |
| --- | --- | --- | --- |
| 1 | 80 | Goose 2 30%, Skunk 2 30%, Goose 3 20%, Shielded Goose 20% | Shielded Goose |
| 2 | 105 | Goose 3 30%, Skunk 2 25%, Skunk 3 15%, Shielded Goose 15%, Shielded Skunk 15% | Shielded Skunk |
| 3 | 130 | Goose 3 30%, Skunk 3 25%, Shielded Goose 20%, Shielded Skunk 25% | Mother Skunk |

These are cost shares, not guaranteed counts. Budgets rise from Capen's
65/85/105, variant-1 enemies are excluded, and durable Skunk 3 arrives in wave
2. Mother Skunk keeps her shared 35 HP and existing three-bomb volleys. Her
fight uses the last wave's roster, at most 65 additional cost, the normal
release rate, a six-living-regular-enemy gate, taper below 50% boss HP and no
new reinforcements at/below 15% HP. Her entrance pauses combat for three seconds.

The full defense wipe is an additional difficulty increase. These values have
not been playtested. The concurrent main checkout is also changing shared
regular-enemy HP; this branch adds no HP overrides and those independent
changes must be accounted for when the team reviews the combined game.

Davis is the last planet in this stage. An empty `nextScene` routes victory to
`GameManager.GoToLevelSelect()`. Union/Capen retain their authored next scenes.
The existing shared HUD already fits the fifth card. The only installed shared
HUD edit supports the empty-next-scene end route; the current Capen grid is preserved.

## Artwork and generator ownership

The scene uses the approved pixel Sun, Ring and SocketFronts sprites and the
twelve measured mounts in `Art/Planets/DavisNova/Pixel/assembly.json`. The ring
and mount coordinates use 0.75 scale so shared 1.5x tower wrappers retain the
approved 2x source-art foundation fit. The gameplay reference radius is 3.96.
Sun/ring/fronts draw at -10/0/35; tower foundations remain under socket fronts.
The original Davis camera IDs and scene GUID are preserved. The menu art
prefab and free-placement art sandbox are preserved outside this gameplay scene.

The sizing reference is the approved pre-gameplay `Pixel/page-review.png`, not
the legacy overlay-gameplay scene. The 0.75 ring scale and 1.5 tower model scale
give the exact approved 2.0 source tower-to-ring ratio. Gameplay roots stay at
unit scale; enemies retain the same shared prefab and regular-enemy 1.5 visual
multiplier as Union/Capen. Shared unit art, stats and wrappers are not resized.
The native-transform review covers five towers at twelve sockets and three aim
poses (180 compositions). Their conservative outer radius is 5.227 world units;
the peak supernova's opaque core covers radius 6.014, including every tower.
Review images, generated source, exact prompt and measurements are under
`Art/Effects/DavisNovaSupernova/`.

User update: the entire Davis gameplay view is now magnified by **1.5x** (50%
larger on screen). `GameplayHUD.worldMagnification` is 1.5 only in Davis;
other scenes default to 1. The camera's fitted size is divided by 1.5, including
after window resizing, player zoom and the boss entrance. Planet, towers,
enemies, projectiles and supernova grow together; HUD size, world geometry,
unit stats, socket fit and blast timing remain unchanged. The offline art
reviews above show proportions; they are not captures of the enlarged viewport.

Run from the repository root:

```powershell
python -B Tools/DavisNova/prepare_gameplay.py
python -B Tools/DavisNova/verify_gameplay.py
python -B Tools/DavisNova/review_visuals.py
```

The generator owns only DavisNova.unity, DavisNova.asset, DavisNovaGameplay.asset
and their necessary new metadata. It reads shared serialization helpers and
Union Prime wiring; it does not invoke the Union or Capen generators. The
verifier reads serialized references, rosters, socket poses, sprite hashes and
metadata without launching Unity.

## Manual team verification

1. Open Davis directly and through Capen victory. Check starting resources,
   five tower cards, twelve sockets, foundation seating, occlusion and ghosts.
2. Start combat. At five seconds remaining check the warning. At zero confirm
   all towers disappear together without refunds, enemies/planet take no blast
   damage, slots reopen and paid replacements work. Build just before a blast
   and include an unpowered tower; both should disappear.
3. Check that setup, safe breaks, pause and the boss entrance hold the countdown;
   it resumes its remainder afterward. Check 2x, repeated blasts and retry reset.
4. Review the three waves and Mother Skunk's volleys/reinforcements with the
   final shared enemy HP. Check boss-kill victory, zero-planet-HP defeat, pause,
   retry, title and victory-to-level-select. Assess whether income gives enough
   rebuilding room after wipes. Sound and final viewport need team review.

Codex performed offline C# compilation and serialized asset/reference checks.
No Unity launch, import validation, Edit/Play Mode tests or standalone build
was performed. Local evidence is in the shared `.git/menu-work/davis-nova/`.
