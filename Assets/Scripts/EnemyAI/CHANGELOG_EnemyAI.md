# Enemy AI: what changed and why

Branch: `enemy-ai` (based on `refactored-level-data`)

## Update 3: moved to the new repo
- The team moved to `kevinl000/UBH-Animal-Space-Invasion`. Its `enemy-ai` branch had everything up to "Let enemy AI choose the enemy mix within the cost budget" but **not** Update 2 below (test suite and fixes). This commit adds it.
- **Merge check with `unique-enemies`**: `unique-enemies` moves `LevelData.cs` / `EnemyData.cs` to `Assets/Scripts/Data/` and `BaseEnemyData.cs` to `Assets/Scripts/Unique Enemies/`. A trial merge with `enemy-ai` (done locally, not pushed) had **no conflicts**: git follows the move and keeps the AI's `LevelData` changes at the new path. Careful: the `LevelData.cs` inside `unique-enemies` is still the old `SpawnRate` version, so don't resolve anything by taking that copy.
- `Tools/EnemyAISim/run.sh` now finds `LevelData.cs` / `EnemyData.cs` anywhere under `Assets/Scripts`, so it works before and after that move (checked on the merged layout: edge cases 20/20, real-asset test runs).
- Where to hook learning into Kevin's enemy classes: keep `mods.Ticket` on `BaseEnemyData`, call `bridge.ReportGone(ticket)` from `Die()`, and `bridge.ReportDamage(ticket, amount)` wherever an enemy's attack hits a tower or the planet.

---

## Update 2: test round
A full simulation test suite now lives in `Tools/EnemyAISim/` (runs without Unity, see its README; latest output in `results.txt`).

### Results (200 runs each, mean ± 95% CI)
**Ablation** (player weak to deer, AoE-heavy defense so the rules point to deer):

| Variant | Deer share of spent cost | Damage per cost, % of oracle |
|---|---|---|
| Random | 15.8 ± 1.2% | 24.4 ± 1.1% |
| Designer shares only | 13.8 ± 1.1% | 22.5 ± 1.0% |
| Rules only | 18.7 ± 1.2% | 27.1 ± 1.2% |
| Learning only | 23.5 ± 1.3% | 31.0 ± 1.3% |
| **Rules + learning** | **32.9 ± 1.5%** | **39.7 ± 1.4%** |
| Oracle (always sends the weak enemy) | 100% | 100% |

Rules and learning each help, and together they do more than the sum. Rules + learning closes 20% of the gap between random and the oracle. The oracle is deliberately out of reach: the caps (rules 1.5x, learning 2x) keep the enemy mix varied instead of sending only deer.

**Strategy switch** (weakness changes from skunks to deer mid-level): the AI's belief flips after a median of 13s (about one 15s step). By 60–90s after the switch, deer share is 22.7% vs 23.3% if deer had been the weakness from the start.

**Wrong rules** (rules push stealth geese, real weakness is deer): learning overrules them after a median of 41s (2/200 runs never did). Deer share 13.7% → 22.7%.

**No weakness**: the mix stays varied (normalized entropy 0.87 vs 0.85 for designer shares only, largest family 42%).

**Edge cases**: 20/20 pass (budget 0, empty or null roster, null snapshot, no towers, full 360° coverage, angle wrap 359→0, negative cost/shares, NaN or negative damage, time going backwards). No exceptions, NaN or infinite loops.

Cost budget exceeded at a spawn: 0 times in every test.

### Fixes from this round
| # | Found by | Problem | Fix |
|---|---|---|---|
| 1 | Real-asset test | Enemy with **cost 0** (current placeholder assets) spent nothing, so one spawned every tick forever (241 in 120s). | Cost ≤ 0 counts as 1 (`LevelData.EffectiveCost`), plus a Console warning. |
| 2 | Real-asset test | **`maxTime` = 0** (current `Level1.asset`) made `GetLevelProgression` return NaN (0/0). | Returns 0 when `maxTime` ≤ 0, plus a warning. |
| 3 | Real-asset test | Silent misconfiguration. | `BeginLevel()` now warns about: maxTime 0, difficulty 0, cost ≤ 0, unknown enemy name, empty roster, missing EnemyData, and the post-boss pause below. |
| 4 | Strategy-switch test | Forgetting rate 0.95 followed a strategy change slowly (78% of the reference 60–90s after the switch). | Default `DecayPerStep` 0.95 → **0.85** (97%), with no significant loss in the steady case (39.7 ± 1.4% vs 40.6 ± 1.4% of oracle). |
| 5 | Edge-case test | The learned factor was documented as up to 3x, but the formula can't go above 2x. | `MaxFactor` set to 2 and docs corrected. Results unchanged. |
| 6 | Test design | No way to switch rules off for comparison. | `useRules` toggle on the bridge (`UseRules` on the director). |

### Found, not changed (team decision)
**Post-boss pause.** The budget is cumulative, so when it drops at the boss to `postBossDifficulty`, nothing can spawn until the curve climbs back past what was already spent (about `maxDifficulty / 2`). With `postBossDifficulty = 0.3 × maxDifficulty`, **no enemies spawn for about half of the second half of the level** (e.g. 180s → 276s in a 360s level). This came from the original LevelData design, not the AI. If you want a short breather instead, set `postBossDifficulty` just above `maxDifficulty / 2`.

### Still needed from the team
- Rename the enemy assets to the enemy names (e.g. `Basic Goose 1`). With the placeholder names, every enemy is treated as Basic Goose 1, so **the AI can't tell them apart and learning does nothing**.
- Fill in `cost` on each enemy and `maxTime` / difficulty on each level.

---

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
| 4 | **Learning had too little data.** A level spawns ~60 enemies; split over 11 types, deer had ~1 result each, so learning barely moved. | Learn per **family** (geese, stealth geese, skunks, deer). Lighter prior Beta(1,1), slower forgetting (0.95 per 15s, later tuned to 0.85, see Update 2). Learned factor range widened to 0.33x–2x (first written as 3x, corrected in Update 2), which is safe because the cost budget still caps difficulty. |

---

## 3. Simulation results (first round, 20 runs; superseded by Update 2)
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
