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
    public string displayName = "Union Prime";
    public string stageLabel = "CAMPUS DEFENSE / TUTORIAL";
    public string nextPlanetName = "Capen Major";
    public LevelData level;
    public TowerData[] towers;
    public GameplayWave[] waves;
    public EnemyData[] finalBosses;
    public int[] starterSlots = { 0, 6 };
    public float spawnInterval = 1f;
    public float bossEntranceDuration = 3f;
    [Header("Final boss reinforcements")]
    [Tooltip("Total extra spawn cost as a fraction of the final wave budget.")]
    [Range(0f, 1f)] public float bossReinforcementBudgetFraction = 0.5f;
    [Tooltip("Normal budget release rate multiplier while the boss is above the taper threshold.")]
    [Min(0f)] public float bossReinforcementRate = 1f;
    [Tooltip("Stop adding enemies while this many regular enemies are already alive, including wave leftovers.")]
    [Min(1)] public int bossReinforcementMaxAlive = 6;
    [Tooltip("Boss health fraction where reinforcement pressure starts falling from the normal rate.")]
    [Range(0f, 1f)] public float bossReinforcementTaperHealth = 0.5f;
    [Tooltip("No new reinforcements at or below this boss health fraction.")]
    [Range(0f, 1f)] public float bossReinforcementStopHealth = 0.15f;
    public string nextScene = "CapenMajor";
}
