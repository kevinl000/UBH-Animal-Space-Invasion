# Union Prime gameplay integration

Open `Assets/Scenes/UnionPrime.unity` in Unity 6000.6.7f1, or launch Union Prime
through the existing destination selector. Press Play, choose a tower, click a
socket to build, and press **Start Wave 1**. Keys 1Ã¢â‚¬â€œ4 select the tutorial towers;
right-click a socket to sell during setup or a break. Escape opens/closes pause.
The pixel-style quick build bar spans the bottom of the screen in one row,
including moon sockets (M1, M2, ...). Hover an empty socket on the planet/moon
or a Quick Build number to preview the selected tower at its rotating socket;
cyan means affordable and red means insufficient
money. Occupied sockets cannot build and show no ghost. Keyboard focus also
previews a socket. The bar observes the same phase permissions as clicking the planet.
Both hover paths share click placement's socket lookup and use one sprite-only
ghost. Its sprite transforms, colors, alpha and draw order come from the actual
tower prefab, so the foreground socket rim occludes it just like a placed tower.

Mouse wheel over the battle zooms in/out; two fingers spread/pinch to zoom.
Planet, towers, enemies, projectiles, effects and placement ghosts share the
camera view, while the HUD stays fixed. The existing fitted view is the starting
zoom (0.75), with limits tightened to 0.625-0.875 (half the previous travel in
each direction). The roster and controls do not
trigger world zoom. Pause, loading, results and the boss entrance block zoom.
After the entrance, the camera returns to the player's chosen zoom.

Pause and 1x/2x are anchored at the top-left of Union Prime's HUD, on the same
canvas as its pause/results overlay. The speed controller starts at 1x before
subscribing to manager events: the loop registers Playing in its early Start,
before ordinary Start methods run. Initializing speed in Start would instead
apply the default zero speed and freeze physics while setup still permits
building. Pause/resume retains the selected speed.

Older planets retain their styled HUD buttons, repositioned to the upper-left
below their title/resource strip. They register the loaded scene with GameManager
for direct-editor play and retry, and follow its pause/resume state. Their result
screen remains separate; configured Union Prime uses only GameplayHUD's results.

The scene is already serialized and wired. **Do not run the legacy Gameplay
scene-setup menu on Union Prime.** Its guard leaves configured scenes alone.

## Where to change gameplay

- `Assets/Scriptable Objects/Levels/UnionPrimeGameplay.asset`: the three waves,
  cost shares/budgets, 20-second spawn windows, 10-second breaks, spawn interval,
  minibosses, four allowed tower types, starter slots, random final-boss pool,
  three-second entrance, final-boss reinforcement limits and next destination.
- `Assets/Scriptable Objects/Levels/UnionPrime.asset`: 50 starting HP, 10 money,
  50 fuel, radius 3.3 and clockwise rotation at 6 degrees/second. All existing
  level data assets and new LevelData defaults start at 50 HP / 10 money / 50 fuel.
- `Assets/Scripts/Gameplay/GameplayLoopController.cs`: phase transitions,
  permissions, boss camera entrance and win/loss. Combat and input consult this
  controller; they do not keep competing wave or result clocks.
- Canonical `EnemyData` and `TowerData` under `Assets/Scriptable Objects/` retain
  the current README stats. All eleven non-boss enemy types now have twice their
  original HP; all eight boss types keep their HP. New/legacy enemy data without
  an authored HP value now defaults to 2 instead of 1. Shiny enemies retain their
  additional 2x HP multiplier. Enemy costs and rewards are unchanged.
  The teammate's existing attack intervals, ranges and
  0.8-world-unit Bomb/Tesla radii remain initial tuning values where the README
  does not specify them. `fireRate` means **seconds between shots**.

Wave 1 releases budget 45 (previously 30): 35% Basic Goose 1, 35% Basic Goose 2,
and 30% Basic Skunk 1 by cost, followed by one Shielded Goose. Wave 2 releases
65 (previously 50): 15% Basic Goose 1, 25% Basic Goose 2, 20% Basic Skunk 1,
15% Basic Skunk 2 and 25% Shielded Goose, followed by one Shielded Skunk.
The variant-2 enemies add durability without changing their global stats or
the director algorithm; they are included in Union Prime's prewarm roster.
Wave 3 retains budget 70 and its previous roster/shares; at its
20-second boundary a 50/50 Healthy Goose/Healthy Skunk encounter replaces the
miniboss. These end encounters are outside the regular spawn budget.

Normal spawns use the teammate's adaptive director and learning, constrained to
that wave's roster. The budget is released linearly, recorded only on successful
spawning, and not carried to the next wave. Each normal wave window ends at
20 seconds; unspent budget is discarded. Learning survives between waves and
into the final boss fight in the same run.

Final bosses receive limited reinforcements after their three-second paused
entrance. The existing adaptive director uses the last wave's roster, shares,
costs, modifiers, spawn angles and learning. An independent spending window
caps the extra cost at 50% of that wave's budget: 35 for Union Prime. Budget
releases at the last wave's normal rate (70 / 20 = 3.5 cost/second here) above
50% boss HP, then tapers linearly to zero at 15% HP. At 32.5% HP it releases
half as quickly. Attempts still happen at most once per configured spawn interval.

New reinforcements wait while six regular enemies are alive, including
leftovers from wave 3. Existing enemies are never removed to enforce this cap.
Crowding and the low-HP cutoff discard unspent reinforcement credit and delay
the next attempt; reopening space cannot release a queued burst. Already
spawned enemies keep fighting. Boss death still wins with surviving planet HP.
The first two wave-end encounters and their clear/break rules are unchanged.
All five reinforcement settings are on LevelGameplayConfig for future planets.
Set the reinforcement budget fraction to zero to disable this feature.

The first two waves clear every enemy before the safe break begins. Projectiles
and short effects are cleared at that boundary without resource drops. Build at
any time except the entrance, pause and results. Sell only during setup/breaks,
for full cost, including the two starter Lasers. Starter Lasers cost nothing to
initialize, but retain their normal data/cost/HP.

Fuel and combat clocks stop outside Wave/Clear/BossFight. Weapons pay their
authored fuel cost once per shot, after finding a target and before the attack.
Idle weapons, cooldowns, frozen weapons, and later projectile/AoE/DOT hits cost
nothing extra. A weapon cannot fire without the entire shot cost, and recovers
when a fuel reward makes the next shot affordable. Regular Radar and Bank are
free; Gamma Radar pays once per damaging pulse with an enemy in range. Oil Rig
still generates +3 fuel/second. This rule is shared across all planets. HP and
resources otherwise persist across waves. No passive refill or health reset is
added. Enemy kills now grant twice the authored fuelDrop, with shiny enemies
retaining their additional 2x bonus. Money rewards are unchanged. Fuel shortage
can still be decisive; the team must review the 50 starting fuel through play.

Boss death wins with surviving planet HP; zero planet HP takes precedence.
Remaining hostiles and projectiles disappear without rewards on victory. Next
Level opens the existing Capen Major scene. Capen and Davis Nova's playable loops
are separate follow-up work. Davis's teammate-authored 40-powered-second
**per-tower** lifespan and Silverman's moon behavior are preserved from the
foundation merge, without reworking their scene integration.

## Artwork and presentation

Union Prime's approved images and twelve measured mounts are reused as
world-space sprites, with the existing foreground lip over tower foundations.
HUD and result menus use native overlay UI generated by `GameplayHUD`; their
content is bound to the loop/configuration. Tower selection lives on the right,
matching the shared PlanetHUD used by the other planets. Future gameplay HUDs
should retain that right-side placement. The original 32 fixed pixel stars are
restored on a non-interactive camera canvas behind the world sprites. The normal
camera reserves the right panel and bottom bar and keeps its original fitted
starting view. Incoming ships spawn just outside the rectangular playfield at
maximum zoom-out, with clearance for their scaled model/shield. The director's
angles, movement and budgets are unchanged. Zooming cannot move the spawn border;
resizing updates it for subsequent arrivals. Summoned minions still appear beside
their parent ship. The entrance suspends combat/rotation and
placement, follows the approaching boss, then returns to the saved camera view.
Pause suspends the entrance too.

Runtime wrappers now reference all nine completed tower visuals, fourteen
completed enemy visuals, ten projectile wrappers and seventeen effect wrappers.
Existing source visual prefabs, sprites, animation clips, controllers and GUIDs
are unchanged. Later units remain unavailable in Union Prime. Undrawn units keep
the teammate's placeholders. All non-boss enemy models receive a shared 1.5x
visual scale at initialization, including future child-model wrappers and
placeholders. Boss identity uses EnemyData.isBoss and keeps the authored size.
The scaling resets from cached authored values on pool reuse; root hit colliders
and combat stats are unchanged. Enlarged outlets follow the models.

Weapon pivots track targets; shots emit from their marker and use the existing
homing behavior. Enemy outlets use the authored marker where available, or a
measured underside marker. Projectile hit circles use visible flight pixels and
respect emission pivots. Shields reset on pool reuse. Bomb/Tesla bursts use the
actual radius; Skunk side puffs use actual secondary damage recipients. Radar
scans/reveal markers, resource activity, Mother Goose calls and Buck's frozen
status are connected on their runtime prefabs. Repeated one-shots reset their
Animator and despawn after the complete clip, including its final blank frame.

## Offline preparation and verification

From the repository root:

```powershell
python -B Tools/UnionPrime/verify_assets.py
# Only to intentionally rebuild the integration's serialized wrappers/scene:
python -B Tools/UnionPrime/prepare_assets.py
```

`prepare_assets.py` owns the generated wrappers and Union Prime scene/config
defaults. It preserves existing metadata and gameplay-root component IDs. It
does **not** preserve manual changes made inside those generated outputs; update
its defaults/mapping first if a rebuild must retain new tuning. Source artwork
and the other scenes are not rebuilt. Ordinary play/Inspector tuning does not
require running it.

Verification is read-only: canonical README numbers, wave restrictions,
local/external references, GUID uniqueness, existing gameplay component identity,
socket coordinates and unchanged approved artwork. Local compile logs and 144
socket/aim compositions live in `.git/menu-work/union-prime/`. C# compilation uses
Roslyn with the installed Unity assemblies; it does not invoke the editor or
execute gameplay.

**No Unity launch, import validation, Unity tests or standalone build was run.**
Offline compilation and geometry checks cannot establish game balance, physics
contacts, Animator playback, viewport readability or live UI interaction.

## Manual Unity review

1. Open Union Prime directly and through the menu. Confirm 50 HP / 10 money /
   50 fuel, two opposite Lasers, twelve sockets and only four available towers.
   Confirm it starts at 1x, the planet rotates, and both starter Lasers follow
   their sockets before pressing Start. Build by clicking the planet and by
   socket number; each real tower must coincide with its ghost and stay mounted.
   Wait in setup: fuel must stay unchanged. Buy/sell; costs/refunds happen once.
   Check initial placement and a sold/rebuilt pooled tower: neither should flash
   at the planet centre before seating. Repeat with a moon socket.
2. Start. Check 20 seconds of spawning, Shielded Goose, complete clearing, then
   exactly 10 safe seconds. Repeat for wave 2/Shielded Skunk. Confirm Basic
   Goose 2 appears from wave 1 and Basic Skunk 2 from wave 2, with no deer,
   stealth or variant-3 enemies. Building works and selling is blocked in combat.
   Compare waves 1/2 with several extra towers: they should need more shots
   and have more sustained pressure. Review actual difficulty manually; these
   are starting tuning values, not a measured win-rate target.
3. Check weapon arcs and moving muzzles, projectile contacts, shield absorption
   and reset, Pest Trap's three-second DOT, nearby Bomb splash and effect reuse.
   Confirm Laser/Bomb/Sniper spend 1 fuel per shot, Pest Trap spends 2, and an
   idle or frozen weapon spends nothing. AoE hits and DOT ticks add no fuel cost.
   Exhaust fuel and obtain a drop: power must recover without a free firing burst.
   On later planets, check SMG/Tesla spend 3 per shot, Ballistica 6, Gamma Radar
   5 per damaging pulse, regular Radar/Bank work at zero fuel, and Oil Rig
   still generates 3/second.
4. At wave 3's boundary, confirm the paused battle and boss zoom/approach/return,
   including pausing/resuming mid-entrance. Across fresh runs review both bosses.
5. Check victory, defeat and a near-simultaneous lethal exchange. Next Level is
   highlighted/focused and opens Capen; Retry and Title use existing managers.
   Retry from setup, pause and defeat must reset resources, starters and waves.
   Check the top-left 1x/2x control, pause/resume at 2x, and Retry returning to 1x.
   Pause/results must cover and block the controls. Open an older planet directly
   and confirm its Pause/Resume, speed, results and Retry use that planet.
   During the final boss, verify regular spawns resume after the entrance using
   wave 3's roster. New adds must wait at six regular enemies; opening a space
   must not trigger a burst. Compare full HP, 32.5% HP and 15% HP: normal,
   half-budget rate, then no new adds. Extra spawn cost must stay at or below 35.
   Pause must stop this budget and Retry must reset it. Waves 1/2 still clear
   completely and give their 10-second break.
6. Inspect all four towers in the twelve sockets through the actual camera at
   16:9 and 4:3. Confirm the right panel and single bottom row do not overlap.
   Wheel/pinch in and out during setup and combat: enemies, towers, shots,
   ghosts and effects must scale together, keep their sockets, and retain their
   combat behavior. Check clicking/building at both limits, resizing the Game
   view while zoomed, and boss entrance/return at each limit. The roster wheel
   must scroll only the roster; pause/results/controls must block zoom.
   Confirm stars stay behind the planet/enemies and do not block building.
   At maximum zoom-out, review entry from all four playfield borders, pooled
   respawns, minibosses and reinforcements. Ships should move into view without
   appearing inside it; zooming must not reposition incoming ships. Confirm
   summoned minions appear beside their parent rather than at the screen edge.
   Hover first/last/rotating sockets both directly and through Quick Build;
   compare ghost seating and orientation with the tower after clicking.
   Move between the two hover paths, off the planet, and over occupied sockets.
   Change towers while hovering, build once,
   and check the ghost disappears on exit, pause, entrance and results. Repeat
   with moon sockets in Silverman Luna. Check foreground seating, aiming extremes, material/sorting,
   font readability, mouse/keyboard focus, small effects and repeated pooled
   shots. Check regular enemies are 50% larger (including after reuse), bosses
   keep their size, and normal/shiny kills give 2x/4x the authored fuelDrop.
   Check the other levels start at 10 money / 50 fuel. Review later prefab
   visuals separately without unlocking them here.
