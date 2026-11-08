using System;
using UnityEngine;

[Serializable]
public class GameplayWave
{
    public float duration = 20f;
    public float restDuration = 10f;
    public float costBudget;
    public LevelEnemyEntry[] roster;
    public EnemyData miniboss;
}

[CreateAssetMenu(menuName = "Planetary Defense/Gameplay Configuration")]
public class LevelGameplayConfig : ScriptableObject
{
    public LevelData level;
    public TowerData[] towers;
    public GameplayWave[] waves;
    public EnemyData[] finalBosses;
    public int[] starterSlots = { 0, 6 };
    public float spawnInterval = 1f;
    public float bossEntranceDuration = 3f;
    public string nextScene = "CapenMajor";
}
