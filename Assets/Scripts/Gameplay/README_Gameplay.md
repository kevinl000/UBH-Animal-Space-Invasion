# Gameplay setup (towers + enemies + level)

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
   - Switches the root canvas to **Screen Space - Camera**. With Screen Space - Overlay, the sprites end up hidden behind the UI.
   - Saves the scene.

## Playing
- Pick a tower by clicking its card in the HUD (locked cards can't be built), or press `1`-`9` / `0`.
- **Left click** near the planet's edge: build in the closest slot. Money is checked, and the tower comes from the PoolManager.
- **Right click** a tower: sell it for half its cost.
- Enemies come from the enemy AI. When the time is up and every enemy is gone, `GameManager.LevelComplete()` is called. If the planet's HP reaches 0, `GameOver()` is called.

## Scripts
| Script | Job |
|---|---|
| `LevelPlanetDefense` | `IPlanetDefense` for real levels. Enemies hit the tower in the slot below them, or the planet (`LevelManager.ReceiveDamage`). Towers ride their slots as the planet turns (Rigidbody2D in FixedUpdate). Handles `TryPlaceTower` / `SellTower` and level complete. |
| `TowerPlacementInput` | Mouse and keyboard building and selling. Reads the HUD's selected card. Turns off the art sandbox's free test lasers. |

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
