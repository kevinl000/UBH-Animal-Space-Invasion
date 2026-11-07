# Enemy AI (Wave Director)

Adaptive layer on top of `LevelData`. LevelData still decides the budget curve and base spawn probabilities; the AI only changes **which** enemy, **where**, and rolls shiny/orbit.

## Files
| File | What it is |
|---|---|
| `EnemyAITypes.cs` | Enums (`EnemyKind`, `TowerKind`), `DefenseSnapshot`, tower/enemy catalogs, suggested enemy weights |
| `AdaptiveWaveDirector.cs` | The AI itself. Pure C#, no UnityEngine |
| `WaveDirectorBridge.cs` | MonoBehaviour that connects the AI to `LevelData` |

## Setup (Unity side)
1. No changes to `EnemyData` needed. The AI reads the enemy type from the **asset name**, ignoring spaces, dashes and case: `Basic Goose 1`, `BasicGoose1` and `basic-goose-1` all work. A name it can't match shows a warning in the Console.
2. Fill `EnemyData.cost` with these weights (or tune them):

| Enemy | Cost | | Enemy | Cost |
|---|---|---|---|---|
| Basic Goose 1 | 3 | | Basic Skunk 1 | 6 |
| Basic Goose 2 | 4 | | Basic Skunk 2 | 8 |
| Basic Goose 3 | 6 | | Basic Skunk 3 | 12 |
| Shielded Goose | 8 | | Shielded Skunk | 14 |
| Stealth Goose | 5 | | Basic Deer | 20 |
| | | | Shielded Deer | 22 |

3. Put `WaveDirectorBridge` on the spawner object and assign `level`.

## Calling it from the spawner
```csharp
bridge.BeginLevel();                                   // at level start, after level.Init()

// every spawn tick:
bridge.Tick(elapsedTime, snapshot);
level.SetEnemyDataSpawnProbability(elapsedTime);
LevelEnemyEntry entry = bridge.PickEntry();   // replaces rolling SpawnProbability yourself; also does SpawnedCount++
if (entry != null)
{
    SpawnModifiers mods = bridge.RollModifiers(entry, snapshot);
    // spawn entry.enemy at mods.SpawnAngleDeg; apply mods.IsShiny (2x HP, 2x drops) and mods.Orbits
}

intentLabel.text = bridge.Intent;                      // e.g. "The flock noticed you have no radar."
if (bridge.ShouldShowBossWarning(prevTime, elapsedTime)) ShowBossWarning();
```

Bridge options in the Inspector:
- `excludeLevelBossFromRoster` (default on): `PickEntry()` never returns the level boss. Turn off if bosses should also come from the normal roster after the halfway point.
- `autoCountSpawns` (default on): turn off only if the spawner already does `SpawnedCount++`.
- `seed`: set to anything but -1 to make a run reproducible.

**Note:** LevelData fixes how many of each enemy spawn in total, so the AI currently only changes the *order* and *position*. To let it shift the actual mix, multiply one line in `SetEnemyDataSpawnProbability`:
`entry.RelativeBudget = Mathf.Lerp(...) * bridge.Director.GetMultiplier(kind);`

`snapshot` is a `DefenseSnapshot`: every tower's `Kind`, world `AngleDeg` on the planet, `Hp`, `IsAlive`, plus `FuelPerSecond`. Building it once per tick is enough.

## What the AI does
- **Adaptive multipliers**, recomputed every 15s, off for the first 20% of the level, capped at 1.5x:
  - no Radar / Gamma Radar → more Stealth Geese
  - 40%+ of attackers are Sniper / Ballistica → more shielded enemies and goose swarms
  - 40%+ of attackers are AoE → more deer
  - half the towers within 30° of another → more skunks
  - fuel per second < 0 → more high-HP enemies
- **Spawn angle**: 60% at the least-covered direction (assumes towers face outward with their FOV), 40% random.
- **Shiny** 5% (bosses 1%). **Orbit** 50% for geese and skunks, deer always.
- Set `seed` on the bridge to make a run reproducible when debugging.

All numbers are in `WaveDirectorSettings` and can be changed in one place.
