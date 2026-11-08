#!/usr/bin/env bash
# Builds and runs the enemy AI simulation tests without Unity.
# Needs Mono (mcs + mono). Usage, from this folder:  ./run.sh [all|1|2|3|4|5|6|7]
# v1.3:  ./run.sh v13   (abstract + combat x2 fuel models + R4a + R4b + R1, raw per-seed -> results13.json)
#        ./run.sh switch (strategy-switch comparison, per seed -> results13_switch.json)
#        ./run.sh r4d   (learning-cap experiment; builds a PATCHED copy of the learner, the game's file is untouched)
# Finds LevelData.cs / EnemyData.cs wherever they live under Assets/Scripts (they have moved before).
set -e
cd "$(dirname "$0")"
ASSETS=../../Assets/Scripts
LEVEL=$(find "$ASSETS" -name LevelData.cs | head -n 1)
ENEMY=$(find "$ASSETS" -name EnemyData.cs | head -n 1)
WAVES=$(find "$ASSETS" -name LevelGameplayConfig.cs | head -n 1)   # wave definitions (GameplayWave), used by the bridge since v1.3
if [ -z "$LEVEL" ] || [ -z "$ENEMY" ]; then echo "Could not find LevelData.cs / EnemyData.cs under $ASSETS"; exit 1; fi
echo "Using $LEVEL and $ENEMY"
SIMSRC="UnityStubs.cs Bench.cs Tests.cs Integ.cs Edge.cs Combat.cs V13.cs"
if [ "${1:-all}" = "r4d" ]; then
  # factor = (theta / prior mean)^gamma lets the learned factor reach 4x / no cap; gamma = 1 is the game's formula
  TMP=$(mktemp -d)
  cp "$ASSETS"/EnemyAI/*.cs "$TMP"/
  sed -i 's|double f = theta / priorMean;|double f = System.Math.Pow(theta / priorMean, SimCap.Gamma);|' "$TMP"/EnemyLearner.cs
  grep -q "SimCap.Gamma" "$TMP"/EnemyLearner.cs || { echo "patch failed"; exit 1; }
  mcs -langversion:7.2 -nowarn:649 -out:sim_cap.exe $SIMSRC "$TMP"/*.cs "$LEVEL" "$ENEMY" $WAVES
  rm -rf "$TMP"
  mono sim_cap.exe r4d
  exit 0
fi
mcs -langversion:7.2 -nowarn:649 -out:sim.exe $SIMSRC "$ASSETS"/EnemyAI/*.cs "$LEVEL" "$ENEMY" $WAVES
mono sim.exe "${1:-all}"
