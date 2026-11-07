#!/usr/bin/env bash
# Builds and runs the enemy AI simulation tests without Unity.
# Needs Mono (mcs + mono). Usage, from this folder:  ./run.sh [all|1|2|3|4|5|6]
# Finds LevelData.cs / EnemyData.cs wherever they live under Assets/Scripts (they have moved before).
set -e
cd "$(dirname "$0")"
ASSETS=../../Assets/Scripts
LEVEL=$(find "$ASSETS" -name LevelData.cs | head -n 1)
ENEMY=$(find "$ASSETS" -name EnemyData.cs | head -n 1)
if [ -z "$LEVEL" ] || [ -z "$ENEMY" ]; then echo "Could not find LevelData.cs / EnemyData.cs under $ASSETS"; exit 1; fi
echo "Using $LEVEL and $ENEMY"
mcs -langversion:7.2 -nowarn:649 -out:sim.exe UnityStubs.cs Bench.cs Tests.cs Integ.cs Edge.cs \
  "$ASSETS"/EnemyAI/*.cs "$LEVEL" "$ENEMY"
mono sim.exe "${1:-all}"
