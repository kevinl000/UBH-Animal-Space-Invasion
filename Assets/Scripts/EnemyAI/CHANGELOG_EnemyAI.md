# Enemy AI: what changed and why

Branch: `enemy-ai` (based on `refactored-level-data`)

## TL;DR
- The enemy AI now decides **which** enemies spawn and **where**. LevelData still decides **how much**: total spawned cost never goes above `GetTotalBudget(time)`.
- The AI learns during play which enemy families hurt the player most (Thompson sampling).
- `LevelData.cs` was changed (agreed with the team). Nothing else outside `Assets/Scripts/EnemyAI/` was touched.

---

## 1. Changes to `Assets/Scripts/LevelData.cs`

### Before
`SetEnemyDataSpawnProbability()` gave each enemy a fixed **target count**:
`targetCount = floor(totalBudget * share / cost)`, minus `SpawnedCount`.
Once an enemy type hit its count, its probability became 0. The AI could only change the **order** enemies came in, never the final mix (e.g. always 10 geese + 2 deer).

### After
| Item | Before | After |
|---|---|---|
| Who decides the mix | LevelData (fixed counts) | Enemy AI |
| Who decides the difficulty | LevelData | LevelData (unchanged) |
| `GetTotalBudget(time)` | Budget curve | Same curve, now read as the **max total cost spawned so far** |
| `startingBudget` / `endingBudget` | Fixed share per enemy | Designer's **starting guess** for the AI. Share 0 = enemy disabled in that level |
| `SetEnemyDataSpawnProbability()` | Computed target counts | Only computes base probabilities from the shares. If all shares are 0, roster enemies start equal |
| `RelativeCount` | Remaining count | Not used anymore (kept so nothing breaks) |

### Added
- `SpentBudget`: total cost of normal enemies spawned this level (reset in `Init()`).
- `GetRemainingBudget(time)` = `GetTotalBudget(time) - SpentBudget`. Can go below 0 right after the post-boss dip; then nothing spawns until the curve catches up.
- `CanAfford(entry, time)`.
- `RecordSpawn(entry)`: `SpawnedCount++` and spends the cost (bosses don't spend).

Unchanged: `GetLevelProgression`, `GetTotalBudget`, `IsBossPhase`, `ShouldSpawnGuaranteedBoss`, `GetBossEnemyData`, all serialized fields.

---

## 2. Problems found in simulation, and the fixes

| # | Problem | Fix |
|---|---|---|
| 1 | **Old: the AI couldn't change the mix.** Fixed target counts meant boosting deer only made them come earlier. | AI now decides the mix within the cost budget (section 1). |
| 2 | **Expensive enemies never spawned.** Every time a little budget built up, a cheap goose used it, so deer (cost 20) never got enough. 0% deer in 40 runs. | The AI picks the next enemy **first**, then waits until the budget can afford it. If it still can't after 10s (`maxWaitForPendingSec`), it picks again. |
| 3 | **Shares were used as count odds.** `startingBudget` / `endingBudget` are shares of **cost**, but were used directly as the chance to pick one enemy, so expensive enemies came more often than designed. | Pick chance is now `share / cost`. |
| 4 | **Learning had too little data.** A level spawns ~60 enemies; split over 11 types, deer had ~1 result each, so learning barely moved. | Learn per **family** (geese, stealth geese, skunks, deer). Lighter prior Beta(1,1), slower forgetting (0.95 per 15s). Learned factor range widened to 0.33x–3x, which is safe because the cost budget still caps difficulty. |

---

## 3. Simulation results
Setup: 180s level, budget 20 → 400, a player who is weak to deer (deer deal ~2x their cost in damage, others ~0.2x). 20 runs each.

| | Deer share of spent cost |
|---|---|
| Rules only | 13% |
| Learning, level 1 | 23% |
| Learning, level 2 (kept learning) | 30% |

- Cost budget exceeded at a spawn: **0 of 40 runs**.
- The HUD line "They've learned deer hurt you the most." showed up in 11 of 20 runs during level 1 (it only appears once the AI is confident).

---

## 4. What the spawner needs to do
```csharp
level.Init();
bridge.BeginLevel();

// every spawn tick (e.g. every 0.5s):
bridge.Tick(elapsedTime, snapshot);
LevelEnemyEntry entry = bridge.PickEntry(elapsedTime);   // null = still saving budget
if (entry != null)
{
    SpawnModifiers mods = bridge.RollModifiers(entry, snapshot);
    // spawn at mods.SpawnAngleDeg, apply mods.IsShiny / mods.Orbits, keep mods.Ticket on the enemy
}

// from the enemy script, for learning:
bridge.ReportDamage(ticket, amount);   // when it damages a tower or the planet
bridge.ReportGone(ticket);             // once, when it dies or despawns
```
- `PickEntry` already calls `level.RecordSpawn(entry)`. Don't call it twice.
- The halfway boss is separate: keep using `ShouldSpawnGuaranteedBoss`.
- Call `PickEntry` once per spawn tick, not in a loop, or enemies will come out in bursts.

## 5. Still open
- Enemy assets are still placeholders (`EasyEnemy`, `MediumEnemt`, `HardEnemy`). Rename them to match the enemy names (e.g. `Basic Goose 1`) so the AI can tell them apart, and fill in `cost`. Placeholder names show a Console warning and are treated as Basic Goose 1.
- No spawner or tower scripts exist yet, so the AI hasn't run inside the actual game.
- Numbers (rule factors, learning speed, wait time) are first guesses. Tune them in `WaveDirectorSettings`, `EnemyLearnerSettings` and the bridge Inspector after playtesting.

Full usage: `README_EnemyAI.md`.
