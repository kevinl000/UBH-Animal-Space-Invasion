# Enemy AI simulation tests

Runs the enemy AI (`Assets/Scripts/EnemyAI/`) and `LevelData` **without Unity**, using small stand-ins for the UnityEngine types (`UnityStubs.cs`). This folder is outside `Assets/` on purpose, so Unity never compiles it.

## Run
Needs Mono (`mcs`, `mono`). From this folder:
```bash
./run.sh        # everything
./run.sh 1      # one test: 1 baselines, 2 strategy switch, 3 real assets, 4 edge cases, 5 wrong rules, 6 no weakness
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

Player model: each enemy family lets through `cost × multiplier × random(0.5–1.5)` damage. "Weak to deer" = deer 2.0, others 0.2.

Latest output: `results.txt`.
