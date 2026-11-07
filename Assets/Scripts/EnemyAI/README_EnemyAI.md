# Enemy AI (Wave Director)

**The AI decides the whole enemy mix. LevelData only sets the cost budget.**
The total cost of spawned enemies never goes above `LevelData.GetTotalBudget(time)`, so the difficulty curve (including the post-boss dip) stays exactly as designed.

## Files
| File | What it is |
|---|---|
| `EnemyAITypes.cs` | Enums (`EnemyKind`, `TowerKind`), `DefenseSnapshot`, tower/enemy catalogs, suggested enemy weights |
| `AdaptiveWaveDirector.cs` | Rule layer, enemy picking, spawn angle, shiny/orbit, boss warning. Pure C# |
| `EnemyLearner.cs` | Online learning layer (Thompson sampling). Pure C# |
| `WaveDirectorBridge.cs` | MonoBehaviour that connects the AI to `LevelData` |
| `Assets/Scripts/LevelData.cs` (changed) | Now tracks spent cost (`SpentBudget`, `GetRemainingBudget`, `CanAfford`, `RecordSpawn`). `SetEnemyDataSpawnProbability` now only gives the AI its starting guess from `startingBudget` / `endingBudget` |

## Setup (Unity side)
1. The AI reads the enemy type from the **EnemyData asset name**, ignoring spaces, dashes and case: `Basic Goose 1`, `BasicGoose1` and `basic-goose-1` all work. A name it can't match shows a warning in the Console.
2. Fill `EnemyData.cost` (or tune these):

| Enemy | Cost | | Enemy | Cost |
|---|---|---|---|---|
| Basic Goose 1 | 3 | | Basic Skunk 1 | 6 |
| Basic Goose 2 | 4 | | Basic Skunk 2 | 8 |
| Basic Goose 3 | 6 | | Basic Skunk 3 | 12 |
| Shielded Goose | 8 | | Shielded Skunk | 14 |
| Stealth Goose | 5 | | Basic Deer | 20 |
| | | | Shielded Deer | 22 |

3. In each LevelData asset, `startingBudget` / `endingBudget` per enemy are now the **designer's starting guess** for the cost share. The AI moves away from it as it learns. Share 0 = that enemy never appears in this level. If every share is 0, all roster enemies start equal.
4. Put `WaveDirectorBridge` on the spawner object and assign `level`.

## Calling it from the spawner
```csharp
level.Init();
bridge.BeginLevel();

// every spawn tick (e.g. every 0.5s), one call:
bridge.Tick(elapsedTime, snapshot);
LevelEnemyEntry entry = bridge.PickEntry(elapsedTime);   // null = still saving budget, try next tick
if (entry != null)
{
    SpawnModifiers mods = bridge.RollModifiers(entry, snapshot);
    // spawn entry.enemy at mods.SpawnAngleDeg; apply mods.IsShiny (2x HP, 2x drops) and mods.Orbits
    // store mods.Ticket on the enemy
}

intentLabel.text = bridge.Intent;   // e.g. "They've learned deer hurt you the most."
if (bridge.ShouldShowBossWarning(prevTime, elapsedTime)) ShowBossWarning();
if (level.ShouldSpawnGuaranteedBoss(prevTime, elapsedTime)) SpawnBoss(level.GetBossEnemyData());
```

`PickEntry` already calls `level.RecordSpawn(entry)` (spends the cost). The halfway boss is not part of this; spawn it with `ShouldSpawnGuaranteedBoss` as before.

**For learning**, report back with the enemy's ticket:
```csharp
bridge.ReportDamage(ticket, amount);   // whenever that enemy damages a tower or the planet
bridge.ReportGone(ticket);             // once, when it dies or despawns
```
Without these two calls the AI still works, it just won't learn.

`snapshot` is a `DefenseSnapshot`: every tower's `Kind`, world `AngleDeg` on the planet, `Hp`, `IsAlive`, plus `FuelPerSecond`. Building it once per tick is enough.

## How it decides
For each candidate enemy: **designer share ÷ cost × rule factor × learned factor**, then it picks one by probability.

It picks the next enemy **first** and waits until the budget can afford it, so expensive enemies (deer) aren't starved by cheap ones (geese). If it still can't afford it after `maxWaitForPendingSec` (10s), it picks again.

- **Rules** (re-checked every 15s, off for the first 20% of the level, max 1.5x):
  - no Radar / Gamma Radar → more Stealth Geese
  - 40%+ of attackers are Sniper / Ballistica → more shielded enemies and goose swarms
  - 40%+ of attackers are AoE → more deer
  - half the towers within 30° of another → more skunks
  - fuel per second < 0 → more high-HP enemies
- **Online learning (multi-armed bandit, Thompson sampling)**: learns per family (geese, stealth geese, skunks, deer). Every enemy that leaves play is scored `damage / (damage + cost)` and updates a Beta belief. Picks sample from these beliefs, so families that hurt the player show up more, while others still get tried. Old results fade every 15s so it re-adapts when the player changes their defense. Learned factor 0.33x–3x (safe, because the cost budget still caps difficulty).
- **Spawn angle**: 60% at the least-covered direction (assumes towers face outward with their FOV), 40% random.
- **Shiny** 5% (bosses 1%). **Orbit** 50% for geese and skunks, deer always.

### Simulation (player weak to deer, 20 runs)
| | Deer share of spent cost |
|---|---|
| Rules only | 13% |
| Learning, level 1 | 23% |
| Learning, level 2 (kept learning) | 30% |

Cost budget exceeded: 0 of 40 runs.

## Bridge options (Inspector)
- `useLearning`: turn off to compare against rules only.
- `keepLearningAcrossLevels`: remember the player between levels.
- `excludeLevelBossFromRoster` (default on): `PickEntry()` never returns the level boss.
- `autoCountSpawns` (default on): turn off only if the spawner calls `level.RecordSpawn(entry)` itself.
- `maxWaitForPendingSec`: how long to save up for a chosen enemy before re-picking.
- `seed`: set to anything but -1 to make a run reproducible.

All other numbers are in `WaveDirectorSettings` / `EnemyLearnerSettings`.
