#!/usr/bin/env bash
# Frame-by-frame enemy behaviour tests without Unity. Needs Mono (mcs + mono). Run from anywhere.
set -e
cd "$(dirname "$0")"
A=../../Assets/Scripts
mapfile -t SRC < <(find "$A/Unique Enemies" "$A/EnemyAI" -name '*.cs'; find "$A" -name LevelData.cs; find "$A" -name EnemyData.cs)
mcs -langversion:7.2 -nowarn:649,414,169 -out:enemytests.exe UnityEngineStub.cs EnemyTests.cs "${SRC[@]}"
mono enemytests.exe ../../README.md
