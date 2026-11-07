using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelEnemyEntry
{
    public EnemyData enemy; // shared enemy type for this roster slot
    public List<BaseEnemyData> enemyInstances; // list of enemies for specific level instance
    public float startingBudget;
    public float endingBudget;
    public bool isLevelBoss; // true only for this level's halfway boss

    public float SpawnRate { get; set; }
    public float RelativeBudget { get; set; }
    public int RelativeCount { get; set; }
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    const float EPSILON = 1.0f; // tolerance for time comparisons
    public int maxDifficulty; // max difficulty for the level
    public float maxTime; // max time for the level in seconds

    public List<LevelEnemyEntry> enemyDataList; // this level's spawn roster

    public int money; // starting money
    public float fuel; // starting fuel

    public float planetMaxHealth;
    public float planetRotationSpeed;
    public float planetGravity;
    public float planetRadius;
    public bool isClockwise; // true = clockwise, false = counter-clockwise

    public float PlanetHealth { get; set; }

    public void Init()
    {
        PlanetHealth = planetMaxHealth;

        if (enemyDataList != null)
        {
            foreach (LevelEnemyEntry entry in enemyDataList)
            {
                if (entry == null)
                {
                    continue;
                }

                entry.RelativeBudget = 0f;
                entry.RelativeCount = 0;
                entry.SpawnRate = 0f;
            }
        }

        SetEnemyDataSpawnRate(0f);

        // possible TODO: if LevelData should own the clock, reset it here. If LevelData should own the money/fuel, reset them here.
        // current money: money is the authored starting amount. If gameplay spends it
        // directly on this field, play mode will dirty the asset. add a runtime
        // CurrentMoney property and restore it from money here.
        // CurrentMoney = money;

        // Current fuel: same pattern as money.
        // CurrentFuel = fuel;

        // elapsed time lives on the spawner / game manager for now.
        // if LevelData should own the clock, reset it here.
        // elapsedTime = 0f;

        // guaranteed-boss tracking lives on the spawner for now (use
        // ShouldSpawnGuaranteedBoss(previousTime, time)). If LevelData should
        // own that one-shot flag, reset it here.
        // hasSpawnedGuaranteedBoss = false;
    }

    public float GetLevelProgression(float time)
    {
        return Mathf.Clamp01(time / maxTime);
    }

    public float GetTotalBudget(float time)
    {
        return (0.1f * maxDifficulty) + (0.9f * maxDifficulty * GetLevelProgression(time));
    }

    public bool IsBossPhase(float time)
    {
        return time > maxTime / 2f;
    }

    public bool ShouldSpawnGuaranteedBoss(float previousTime, float time)
    {
        return (time <= maxTime / 2f + Mathf.Epsilon) && (time >= maxTime / 2f - Mathf.Epsilon);
    }

    public EnemyData GetBossEnemyData()
    {
        if (enemyDataList == null)
        {
            return null;
        }

        foreach (LevelEnemyEntry entry in enemyDataList)
        {
            if (entry != null && entry.isLevelBoss)
            {
                return entry.enemy;
            }
        }

        return null;
    }

    public void SetEnemyDataSpawnRate(float time)
    {
        if (enemyDataList == null)
        {
            return;
        }

        bool includeLevelBoss = IsBossPhase(time);
        float progression = GetLevelProgression(time);
        float totalBudget = GetTotalBudget(time);
        int totalEnemyCount = 0;

        foreach (LevelEnemyEntry entry in enemyDataList)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.isLevelBoss && !includeLevelBoss)
            {
                entry.RelativeBudget = 0f;
                entry.RelativeCount = 0;
                entry.SpawnRate = 0f;
                continue;
            }

            int cost = entry.enemy != null ? entry.enemy.cost : 0;
            entry.RelativeBudget = 1f - ((entry.startingBudget - entry.endingBudget) * progression);
            entry.RelativeCount = cost > 0
                ? Mathf.FloorToInt(totalBudget * entry.RelativeBudget / cost)
                : 0;
            totalEnemyCount += entry.RelativeCount;
        }

        foreach (LevelEnemyEntry entry in enemyDataList)
        {
            if (entry == null)
            {
                continue;
            }

            entry.SpawnRate = totalEnemyCount > 0
                ? (float)entry.RelativeCount / totalEnemyCount
                : 0f;
        }
    }

}
