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

    public int SpawnedCount { get; set; }//stores the total number of of enemy type that has spawned.
    public float SpawnProbability { get; set; }
    public float RelativeBudget { get; set; }
    public int RelativeCount { get; set; }
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    public int startingDifficulty; // starting difficulty of the level 
    public int postBossDifficulty; // difficulty of the level after the boss
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
                entry.SpawnProbability = 0f;
                entry.SpawnedCount = 0;
            }
        }

        SetEnemyDataSpawnProbability(0f);

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
        float progression = GetLevelProgression(time);

        // before the half way time, increase the budget at an increasing rate reaching half of max difficulty at half time
        if (progression <= 0.5f)
        {
            float phase1Progression = progression * 2f;
            float curve = phase1Progression * phase1Progression;
            return Mathf.Lerp(startingDifficulty, maxDifficulty * 0.5f, curve);
        }
        else // after the halfway time, dip the difficulty to postBossDifficulty then increase it at an increasing rate to max difficulty
        {
            float phase2Progression = (progression - 0.5f) * 2f;
            float curve = phase2Progression * phase2Progression;
            return Mathf.Lerp(postBossDifficulty, maxDifficulty, curve);
        }
    }

    public bool IsBossPhase(float time)
    {
        return time > maxTime / 2f;
    }

    public bool ShouldSpawnGuaranteedBoss(float previousTime, float time)
    {
        float bossTime = maxTime / 2f;
        // Returns true exactly on the frame the timer crosses the halfway point
        return previousTime < bossTime && time >= bossTime;
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

    public void SetEnemyDataSpawnProbability(float time)
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
                entry.SpawnProbability = 0f;
                entry.SpawnedCount = 0;
                continue;
            }

            int cost = entry.enemy != null ? entry.enemy.cost : 0;
            entry.RelativeBudget = Mathf.Lerp(entry.startingBudget, entry.endingBudget, progression);//linearly interpolates from starting budget to ending budget
            int targetCount = cost > 0 ? Mathf.FloorToInt(totalBudget * entry.RelativeBudget / cost) : 0;//calculates the targeted amount of a enemies of a type that should be spawned at that time
            entry.RelativeCount = Mathf.Max(0, targetCount - entry.SpawnedCount); // gets the actuall count of needed enemies to spawn of that type (factoring enemies that have been spawned already)
            totalEnemyCount += entry.RelativeCount;
        }

        foreach (LevelEnemyEntry entry in enemyDataList)
        {
            if (entry == null)
            {
                continue;
            }

            entry.SpawnProbability = totalEnemyCount > 0
                ? (float)entry.RelativeCount / totalEnemyCount
                : 0f;
        }
    }

}
