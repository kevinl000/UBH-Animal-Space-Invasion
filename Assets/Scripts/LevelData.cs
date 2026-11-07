using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelEnemyEntry
{
    public EnemyData enemy; // shared enemy type for this roster slot
    public List<BaseEnemyData> enemyInstances; // list of enemies for specific level instance
    public float startingBudget; // designer's starting share for this enemy (used as the AI's prior)
    public float endingBudget;   // designer's ending share for this enemy (used as the AI's prior)
    public bool isLevelBoss; // true only for this level's halfway boss

    public int SpawnedCount { get; set; }//stores the total number of of enemy type that has spawned.
    public float SpawnProbability { get; set; } // base (prior) probability from the designer's shares; the enemy AI adjusts it
    public float RelativeBudget { get; set; }   // designer's share at the current time (Lerp of starting/ending budget)
    public int RelativeCount { get; set; }      // no longer used for spawning decisions (kept for compatibility)
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

    // Total cost of all normal enemies spawned so far this level.
    // The enemy AI decides WHICH enemies to spawn, but SpentBudget can never go above GetTotalBudget(time).
    public float SpentBudget { get; private set; }

    public void Init()
    {
        PlanetHealth = planetMaxHealth;
        SpentBudget = 0f;

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
        if (maxTime <= 0f)
        {
            return 0f; // maxTime not set yet: avoid 0/0 = NaN
        }

        return Mathf.Clamp01(time / maxTime);
    }

    // Cost used for budgeting. A cost of 0 or less would let an enemy spawn for free every tick, so it counts as 1.
    public static int EffectiveCost(EnemyData enemy)
    {
        return enemy != null ? Mathf.Max(1, enemy.cost) : 1;
    }

    // Total cost allowed to have been spawned by this time (cumulative).
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

    // How much cost is still available right now. Can be <= 0 (e.g. right after the post-boss dip):
    // then nothing spawns until the curve catches up.
    public float GetRemainingBudget(float time)
    {
        return GetTotalBudget(time) - SpentBudget;
    }

    // True if this entry is allowed to spawn right now within the cost budget.
    public bool CanAfford(LevelEnemyEntry entry, float time)
    {
        if (entry == null || entry.enemy == null)
        {
            return false;
        }

        return EffectiveCost(entry.enemy) <= GetRemainingBudget(time);
    }

    // Call once for every normal enemy that actually spawns.
    public void RecordSpawn(LevelEnemyEntry entry)
    {
        if (entry == null || entry.enemy == null)
        {
            return;
        }

        entry.SpawnedCount++;
        if (!entry.isLevelBoss)
        {
            SpentBudget += EffectiveCost(entry.enemy);
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

    // Computes each enemy's BASE probability from the designer's shares (startingBudget -> endingBudget).
    // This is only the starting point: the enemy AI multiplies it by what it has learned and
    // picks the enemy, as long as the enemy's cost fits in GetRemainingBudget(time).
    // The halfway boss is never included here; spawn it with ShouldSpawnGuaranteedBoss.
    public void SetEnemyDataSpawnProbability(float time)
    {
        if (enemyDataList == null)
        {
            return;
        }

        float progression = GetLevelProgression(time);
        float totalShare = 0f;

        foreach (LevelEnemyEntry entry in enemyDataList)
        {
            if (entry == null)
            {
                continue;
            }

            entry.RelativeCount = 0;

            if (entry.isLevelBoss || entry.enemy == null)
            {
                entry.RelativeBudget = 0f;
                continue;
            }

            entry.RelativeBudget = Mathf.Max(0f, Mathf.Lerp(entry.startingBudget, entry.endingBudget, progression));//linearly interpolates from starting budget to ending budget
            totalShare += entry.RelativeBudget;
        }

        // If no shares were authored, treat every normal enemy in the roster equally.
        int normalCount = 0;
        if (totalShare <= 0f)
        {
            foreach (LevelEnemyEntry entry in enemyDataList)
            {
                if (entry != null && !entry.isLevelBoss && entry.enemy != null)
                {
                    normalCount++;
                }
            }
        }

        foreach (LevelEnemyEntry entry in enemyDataList)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.isLevelBoss || entry.enemy == null)
            {
                entry.SpawnProbability = 0f;
            }
            else if (totalShare > 0f)
            {
                entry.SpawnProbability = entry.RelativeBudget / totalShare; // an enemy with share 0 is disabled for this level
            }
            else
            {
                entry.SpawnProbability = normalCount > 0 ? 1f / normalCount : 0f;
            }
        }
    }

}
