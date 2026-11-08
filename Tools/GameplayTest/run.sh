#!/usr/bin/env bash
# End-to-end gameplay tests without Unity: towers + enemies + AI + LevelManager + LevelPlanetDefense. Needs Mono.
set -e
cd "$(dirname "$0")"
A=../../Assets/Scripts
mapfile -t SRC < <(find "$A/Unique Enemies" "$A/Unique Towers" "$A/EnemyAI" "$A/Pooling" "$A/Gameplay" "$A/Data" -name '*.cs'; echo "$A/Managers/LevelManager.cs"; echo "$A/Managers/GameManager.cs")
mcs -langversion:7.2 -nowarn:649,414,169 -out:gameplaytests.exe ../EnemyBehaviourTest/UnityEngineStub.cs UiStubs.cs GameplayTests.cs "${SRC[@]}"
mono gameplaytests.exe

# type-check the editor scripts too (compile only)
mcs -langversion:7.2 -nowarn:649,414,169,67,108,114 -define:UNITY_EDITOR -target:library -out:editorcheck.dll ../EnemyBehaviourTest/UnityEngineStub.cs UiStubs.cs EditorStubs.cs ../../Assets/Editor/Gameplay/GameplaySetup.cs ../../Assets/Editor/Enemies/EnemyDataGenerator.cs "${SRC[@]}" && echo "Editor scripts: type check OK"
rm -f gameplaytests.exe editorcheck.dll
