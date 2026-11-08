# Capen Major gameplay

Capen Major now uses the shared `GameplayLoopController`, `GameplayHUD`, director,
pooling and tower placement used by Union Prime. The scene uses the approved
eight sprite layers and all sixteen measured sockets. Artwork, presentation
prefab, import metadata and scene GUID are preserved; the legacy canvas demo and
its free-placement sandbox are replaced in this gameplay scene only.

Starting HP is **75**, money **10**, fuel **50**, with two free Lasers at opposite
sockets (1 and 9). Laser, Bomb, Sniper and Pest Trap are joined by Radar, Oil Rig
and Bank. The seven cards use two columns in the right sidebar, with portraits
and selected support effects shown in the details panel. Hotkeys 1–7 select them
in that order; all seven work with socket hover and quick build.

| Support tower | HP | Cost | Fuel | Effect |
| --- | --- | --- | --- | --- |
| Radar | 10 | 5 | Free | Reveals stealth enemies in its 90-degree arc |
| Oil Rig | 15 | 5 | +3/s | Generates fuel |
| Bank | 15 | 5 | Free | Generates $3/s |

Support towers use their existing projectile-free runtime behavior and generate
or scan only during combat. Union Prime keeps its four tower options and original
single-column layout. Support unlocks do not add stealth enemies to Capen's roster.

Shots consume fuel, kills give the existing doubled fuel rewards. Setup, clear,
full-refund safe breaks, pause, speed, zoom, socket/quick-build ghosts and results
use the shared runtime. All three spawn windows last 20 seconds, with 10-second
breaks after clearing waves 1 and 2. Planet radius is 4.9 world units, matching
the larger authored artwork; rotation retains the Union Prime runtime baseline.

| Wave | Cost budget | Cost shares | End encounter |
| --- | --- | --- | --- |
| 1 | 65 | Goose 2 45%, Skunk 2 35%, Shielded Goose 20% | Shielded Goose |
| 2 | 85 | Goose 2 30%, Skunk 2 30%, Goose 3 20%, Shielded Goose 20% | Shielded Skunk |
| 3 | 105 | Goose 2 25%, Skunk 2 25%, Goose 3 20%, Shielded Goose 15%, Shielded Skunk 15% | Mother Goose |

These are director cost shares, not guaranteed enemy counts. No variant-1 enemy
is permitted. Shared enemy HP, rewards and costs are unchanged. All required
enemies, including Mother Goose's Basic Goose 3 flock, are in the prewarm roster.

Mother Goose has her existing HP and art, orbits, and summons four Basic Goose 3
every eight seconds (first flock after four seconds at her orbit). Her own flocks
remain unrestricted, as requested. The director separately continues Wave 3's
roster during the final fight, with at most 52.5 extra cost, normal release rate,
a six-living-regular-enemy gate, taper from 50% boss HP, and no spawns at 15% HP.
Her flocks count toward that director gate but can exceed six themselves and
continue near death. Boss death wins if the planet survives; remaining combat
objects are cleared without rewards. Victory continues to Davis Nova.

`prepare_gameplay.py` owns the Capen scene and its two new level data assets. Run
from the repo root with `python -B Tools/CapenMajor/prepare_gameplay.py` only when
intentionally regenerating them. It reads shared wiring from Union Prime but
does not regenerate or write Union Prime, wrappers, source art or other planets.
`python -B Tools/CapenMajor/verify_gameplay.py` checks serialized references,
rosters, resources, socket poses and artwork hashes without running Unity.

Manual review by the team (no Unity execution/tests performed by Codex):

1. Open Capen Major directly and through Union Prime's victory button. Confirm
   Capen labels, 75 HP, starting resources, two opposite Lasers and sixteen slots.
   Confirm all seven build cards fit without overlapping details or Start. Select
   Radar/Oil Rig/Bank by card and keys 5/6/7, then build with socket/quick build.
   In combat, check Radar's scan, Oil Rig's +3 fuel/s and Bank's $3/s; check that
   income stops in setup, breaks and pause. Radar and Bank consume no fuel.
2. Build at upper, side and lower sockets; check foundation seating/occlusion,
   hover ghosts, quick build, zoom, rotation and no placement flash. Check pause
   and 2x speed without moving the HUD.
3. Play all waves. Confirm only the documented types, Goose 3 from Wave 2,
   wave-end shields, full clearing and exactly 10-second breaks. Assess the new
   budgets with several additional towers; difficulty is not empirically tuned.
4. Check Mother Goose's paused camera entrance, flock/effect playback, unrestricted
   flocks, director taper, boss kill victory, planet-zero defeat and retry reset.
   Confirm Next opens Davis Nova and Title returns to the main menu.
