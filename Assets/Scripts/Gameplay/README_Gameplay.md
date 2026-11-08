# Gameplay setup (towers + enemies + level)

## Union Prime integration (2026-11-08)

Union Prime now uses the centralized `GameplayLoopController` and the
`UnionPrimeGameplay` configuration asset: setup, three timed waves, clearing,
safe full-refund breaks, random tutorial boss, entrance camera and results.
Its serialized world-space scene is ready to open; it does not use the legacy
automatic start / halfway boss / time-up victory described below. See
`Tools/UnionPrime/README.md` for controls, tuning, required teammate-code changes,
art bindings, offline verification and the team's manual Unity checklist.

All completed art is bound to runtime prefab children while tutorial unlocks
remain restricted. Existing art assets and GUIDs are preserved. The legacy
prefab/data menu keeps assigned projectile overrides, and scene setup refuses
to overwrite a scene with a configured gameplay loop.

All planets now charge weapons once per shot rather than per second. The
negative fuelIntake value is a shot cost; Gamma Radar pays per damaging pulse
with a target. No target, cooldown, freezing, AoE recipients and later DOT ticks
do not consume fuel. Radar and Bank are free; Oil Rig still generates fuel per
second. Both runtime HUDs show these units. The AI's fuel-per-second snapshot
is an estimate at full firing rate and does not deduct resources.

Positioned pool spawns set the body and visible transform before activation and
reset interpolation, avoiding a flash at the previous pooled/planet-centre pose.

No Unity execution or Unity tests were performed for this integration. The
historical test instructions below belong to the teammate's earlier foundation.

## Capen Major integration (2026-11-08)

Capen Major now uses the same configured loop and world-space combat, with its
eight existing art layers and sixteen authored sockets. Its own configuration
starts at 75 HP, uses Basic Goose 2 / Basic Skunk 2 as common enemies, adds shields
and Basic Goose 3, and ends with Mother Goose. No variant-1 enemy is permitted.
The shared HUD takes the planet title and next destination from the configuration;
Union Prime retains its existing labels and Capen victory continues to Davis Nova.
See `Tools/CapenMajor/README.md` for exact budgets, Mother Goose's unrestricted
flocks, generator ownership, offline checks and the manual verification checklist.

## Legacy setup for other scenes

How a level scene is put together so towers, enemies, the enemy AI and the LevelManager work as one game.

## Two menu items do the setup

1. **Tools > Gameplay > 1. Build All Prefabs and Tower Data** (once per project).
   - Creates a prefab for each README tower, enemy and projectile in `Assets/Prefabs/Gameplay/`. Each prefab has a Rigidbody2D, a trigger CircleCollider2D, the behaviour script, and the art as a child.
   - Uses the art that exists: goose, skunk, Bomb tower, Laser base, goose poop, skunk bomb and laser bolt. Everything else gets a coloured placeholder circle.
   - Creates the 11 README **TowerData** assets in `Assets/Scriptable Objects/Towers/README/` and fills in `prefab` / `projectilePrefab`.
   - Fills `prefab` / `projectilePrefab` on the 19 README **EnemyData** assets, plus the boss fields (Mother Goose flock, Endangered Deer guard, Animal Ship cargo).
   - Prefabs that already exist are kept. To rebuild one, delete it and run the menu again. To swap in new art, open the prefab and replace its `Art` child.
2. **Tools > Gameplay > 2. Add Gameplay To Open Level Scene** (once per level scene, with the scene open).
   - Adds a `Gameplay` object with `PoolManager`, `LevelPlanetDefense`, `WaveDirectorBridge`, `EnemySpawner` and `TowerPlacementInput`, all wired to the scene's `LevelManager`.
   - If the LevelManager has no LevelData, it creates one named after the scene. Only empty fields are filled in: README roster by level, the level's boss, time, money, fuel, HP and rotation.
   - Keeps the HUD canvas on **Screen Space - Overlay**, so the text stays sharp and the menus draw above the enemies. It hides the HUD's plain full-screen `Background` image and has the camera clear to the same colour, so the sprites show between the background and the planet / HUD. The planets are UI, so `UiSpace` converts their pixel positions into the camera's world.
   - Saves the scene.

## Playing
- Pick a tower by clicking its card in the HUD (locked cards can't be built), or press `1`-`9` / `0`.
- **Left click** near the planet's edge: build in the closest slot. Money is checked, and the tower comes from the PoolManager.
- **Right click** a tower: sell it for half its cost.
- Enemies come from the enemy AI. When the time is up and every enemy is gone, `GameManager.LevelComplete()` is called. If the planet's HP reaches 0, `GameOver()` is called.

## Planet rules (added by menu 2 in those scenes)
- **Davis Nova: `TowerLifespan`.** Every tower lives `lifespanSeconds` (40). It flashes red for the last `warningSeconds` (5), then explodes: `explosionDamage` (3) to enemies within `explosionRadius` (1.2 world units), a pooled `Tower Explosion` effect, and the tower is destroyed (`TowerEvents.TowerDestroyed`). A tower without fuel stops ageing.
- **Silverman Luna: `MoonOrbit`.** The `Moon` art gets a kinematic Rigidbody2D and orbits the planet at its authored distance (`orbitDegreesPerSecond` 8) while spinning (`spinDegreesPerSecond` -20). It has 4 slots: click the moon to build there. Moon towers ride the moon, shoot like any tower, use fuel, and count in the enemy AI's view of the defense.

## Scripts
| Script | Job |
|---|---|
| `LevelPlanetDefense` | `IPlanetDefense` for real levels. Enemies hit the tower in the slot below them, or the planet (`LevelManager.ReceiveDamage`). Towers ride their slots as the planet turns (Rigidbody2D in FixedUpdate). Handles `TryPlaceTower` / `SellTower` and level complete. |
| `TowerPlacementInput` | Mouse and keyboard building and selling. Reads the HUD's selected card. Turns off the art sandbox's free test lasers. Clicks on the moon go to the moon. |
| `TowerLifespan` | Davis Nova: towers explode at the end of their lifespan. |
| `MoonOrbit` | Silverman Luna: the orbiting, spinning moon with 4 tower slots. |
| `UiSpace` | Converts a UI object's position / size on an overlay canvas (screen pixels) into the camera's world. |
| `PooledEffect` | A one-shot visual from the PoolManager that returns itself after `lifetime`. |

## Fixes made while combining
- Building a tower **spent** money instead of adding it, and towers could be built without enough money.
- `fov` was used as a distance. It's now the README angle around the tower's outward direction. Optional `TowerData.range` limits distance.
- Radar and Resource towers never ran, because their attack type is None and the base `Tick` skips None. The Fuel resource also raised a money tick.
- Pest Trap now applies a real 1/s DoT for 3 s that ignores the shield. Before, it dealt one tick.
- `LevelManager.Awake` overwrote `planetBody` with a missing component, which caused a NullReferenceException. `ReceiveDamage` now returns the damage dealt, which the enemy AI learns from.
- A tower with no projectile prefab now hits instantly instead of doing nothing.

## Tests (no Unity needed)
`Tools/GameplayTest/run.sh` covers:
- Building, cost, slots and rotation
- Lasers killing in FOV and ignoring enemies behind the planet
- Enemies damaging towers and planet HP
- Selling and pooling
- The Bank, Oil Rig and fuel economy
- Pest Trap DoT through the shield
- Radar revealing stealth
- Ballistica's 20% damage
- A full level with the AI ending in LevelComplete

It also type-checks the editor scripts.

When the full-level test had no Radar, the enemy AI learned to send only Stealth Geese and won. That's the AI working as intended.
