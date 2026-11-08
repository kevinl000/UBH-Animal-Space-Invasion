# Enemy AI simulation tests

Runs the enemy AI (`Assets/Scripts/EnemyAI/`) and `LevelData` **without Unity**, using small stand-ins for the UnityEngine types (`UnityStubs.cs`). This folder is outside `Assets/` on purpose, so Unity never compiles it.

## Run
Needs Mono (`mcs`, `mono`). From this folder:
```bash
./run.sh        # everything
./run.sh 1      # one test: 1 baselines, 2 strategy switch, 3 real assets, 4 edge cases, 5 wrong rules, 6 no weakness, 7 combat model
```
Test 3 reads the real assets in `Assets/Scriptable Objects/` (override with `UBH_ASSETS=...`). `run.sh` finds `LevelData.cs` / `EnemyData.cs` anywhere under `Assets/Scripts`.

## What each test does
| # | Test | Question it answers |
|---|---|---|
| 1 | Baselines (random, designer only, rules only, learning only, rules + learning, oracle), 200 runs, mean ± 95% CI | How much do the rules and the learning each help? |
| 2 | Player's weakness switches from skunks to deer mid-level, 3 forgetting rates | How fast does the AI follow a strategy change? |
| 3 | The team's real `Level1.asset` and enemy assets through the bridge | Does it break at the Unity boundary? |
| 4 | 20 edge cases (budget 0, empty roster, null snapshot, angle wrap, NaN input...) | No exceptions, NaN or infinite loops? |
| 5 | The rules point to the wrong enemy | Can learning overrule bad rules, and how fast? |
| 6 | Player has no weakness | Does the mix stay varied instead of collapsing onto one enemy? |
| 7 | Combat model from the README stats (`Combat.cs`), 5 tower loadouts, 100 runs | Does the AI still help when the weakness comes out of the towers instead of being assumed? |

Tests 1–6 player model: each enemy family lets through `cost × multiplier × random(0.5–1.5)` damage. "Weak to deer" = deer 2.0, others 0.2.

Test 7 runs real combat on a rotating 12-slot planet with the README's tower and enemy stats. Anything the README doesn't specify (fire rates, enemy attack timing, tower range, level budget...) is an assumption at the top of `Combat.cs` (`CombatAssumptions`). Update those once the game has real values. The loadouts are fuel-balanced the way a player would build them.

Latest output: `results.txt`.

## v1.3 report (paired statistics)
```bash
./run.sh all      # v1.2 suite (regression check, C5)
./run.sh v13      # abstract + combat (old and new fuel model) + R4a + R4b + R1, raw per-seed values -> results13.json
./run.sh switch   # strategy-switch comparison per seed -> results13_switch.json
./run.sh r4d      # learning cap 2x/4x/none on a PATCHED copy of the learner (the game's file is untouched) -> results13_r4d.json
python3 report13.py           # EnemyAI_Report_v1.3.pdf; commit id from git (a '+' means uncommitted changes)
```
- Same seeds 0-199 in every condition; each comparison is judged by the paired difference (bootstrap 10,000 + t), not by overlapping CIs.
- R1 (`Integ.cs`) checks every LevelData and wave config in `Assets/Scriptable Objects/Levels` and runs them through the bridge (level mode and wave mode).
- Needs Python 3 with matplotlib, numpy and scipy.

## PDF report (with version comparison, v1.0-v1.2)
```bash
./run.sh all                                   # writes results.txt-style tables to the console and results.json
python3 report.py --version v1.1 --note "what changed in the AI"
```
- Writes `EnemyAI_Report_<version>.pdf` (5 pages: headline numbers + ablation, AI vs algorithm only (rules without learning) across every test, adapting to a strategy change + wrong rules, robustness, combat model).
- Also saves `history/<version>.json`. The ablation chart puts the last 3 saved versions side by side, and the version table lists up to 8, so you can see what a change did.
- Use a new `--version` name every time you change the AI. Reusing a name overwrites that entry.
- Needs Python 3 with matplotlib (`pip install matplotlib`).
