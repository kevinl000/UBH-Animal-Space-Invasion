// Enemy AI (Wave Director) - Unity glue.
//
// Connects AdaptiveWaveDirector to the team's LevelData. No changes to EnemyData needed:
// the enemy kind is read from the EnemyData ASSET NAME, ignoring spaces, dashes and case.
//   e.g. an asset named "Basic Goose 1" or "BasicGoose1" -> EnemyKind.BasicGoose1
// Unknown names are logged once and treated as BasicGoose1 (no adaptive bonus).
//
// The AI decides the whole enemy mix. LevelData only sets the COST budget:
// the total cost of spawned enemies never goes above level.GetTotalBudget(time).
//
// Usage from the spawner:
//   bridge.BeginLevel();                                  // level start (after level.Init())
//   every spawn tick (e.g. every 0.5s; one enemy per call):
//   bridge.Tick(elapsedTime, snapshot);
//   LevelEnemyEntry entry = bridge.PickEntry(elapsedTime); // null = nothing affordable right now, try next tick
//   if (entry != null) {
//       SpawnModifiers mods = bridge.RollModifiers(entry, snapshot);
//       // spawn entry.enemy at mods.SpawnAngleDeg, apply mods.IsShiny / mods.Orbits
//   }
//   intentLabel.text = bridge.Intent;                     // HUD line
//
// Learning (so the AI finds out which enemies hurt THIS player):
//   store mods.Ticket on the spawned enemy, then
//   bridge.ReportDamage(ticket, amount);   // whenever that enemy damages a tower or the planet
//   bridge.ReportGone(ticket);             // once, when it dies or despawns
//
// PickEntry() records the spawn in LevelData (SpawnedCount and SpentBudget) for you
// (turn off with autoCountSpawns, then call level.RecordSpawn(entry) yourself).

using System;
using System.Collections.Generic;
using UnityEngine;
using EnemyAI;

public class WaveDirectorBridge : MonoBehaviour
{
    public LevelData level;

    [Tooltip("-1 = random every run. Any other value makes runs reproducible for debugging.")]
    public int seed = -1;

    [Tooltip("If true, PickEntry() never returns the level boss (spawn it with ShouldSpawnGuaranteedBoss instead). " +
             "Set false if bosses should also come from the normal roster after the halfway point.")]
    public bool excludeLevelBossFromRoster = true;

    [Tooltip("If true, PickEntry() calls level.RecordSpawn(entry), which spends the enemy's cost from the budget. " +
             "Set false only if the spawner calls RecordSpawn itself.")]
    public bool autoCountSpawns = true;

    [Tooltip("The AI picks the next enemy first and waits until the budget can afford it, so expensive enemies " +
             "are not starved by cheap ones. If it still can't afford it after this many seconds, it picks again.")]
    public float maxWaitForPendingSec = 10f;

    [Tooltip("Online learning on top of the rules. Turn off to compare rules-only behavior.")]
    public bool useLearning = true;

    [Tooltip("Hand-written counter rules (e.g. no radar -> more stealth geese). Turn off to compare learning-only behavior.")]
    public bool useRules = true;

    [Tooltip("If true, the AI keeps what it learned about the player when a new level starts.")]
    public bool keepLearningAcrossLevels = true;

    private AdaptiveWaveDirector director;
    private readonly List<AdaptiveWaveDirector.Candidate> candidates = new List<AdaptiveWaveDirector.Candidate>();
    private readonly Dictionary<EnemyData, EnemyKind> kindCache = new Dictionary<EnemyData, EnemyKind>();
    private LevelEnemyEntry pending;   // the enemy the AI has decided to send next
    private float pendingSince;
    private GameplayWave wave;
    private float waveSpent;
    private float waveBudget;
    public float WaveSpent => waveSpent;
    public float WaveRemainingBudget => Mathf.Max(0f, waveBudget - waveSpent);

    public void BeginWave(GameplayWave definition, float budgetFraction = 1f)
    {
        wave = definition;
        waveBudget = definition.costBudget * Mathf.Clamp01(budgetFraction);
        waveSpent = 0f;
        pending = null;
    }

    public LevelEnemyEntry PickWaveEntry(float releasedBudget)
    {
        if (wave == null) return null;
        float ceiling = waveBudget - waveSpent;
        if (pending == null)
        {
            candidates.Clear();
            foreach (LevelEnemyEntry entry in wave.roster)
            {
                if (entry == null || entry.enemy == null || entry.startingBudget <= 0f) continue;
                int cost = LevelData.EffectiveCost(entry.enemy);
                candidates.Add(new AdaptiveWaveDirector.Candidate { Kind = KindOf(entry.enemy), Cost = cost,
                    BaseProbability = entry.startingBudget / cost, Tag = entry });
            }
            var chosen = director.PickEnemy(candidates, ceiling);
            pending = chosen != null ? (LevelEnemyEntry)chosen.Tag : null;
        }
        return pending != null && LevelData.EffectiveCost(pending.enemy) <= releasedBudget - waveSpent ? pending : null;
    }

    public void RecordWaveSpawn(LevelEnemyEntry entry)
    {
        waveSpent += LevelData.EffectiveCost(entry.enemy);
        pending = null;
    }

    /// <summary>The enemy the AI is saving budget for (null if none). Handy for a debug label.</summary>
    public LevelEnemyEntry Pending
    {
        get { return pending; }
    }

    public string Intent
    {
        get { return director != null ? director.CurrentIntent : ""; }
    }

    public AdaptiveWaveDirector Director
    {
        get { return director; }
    }

    private void Awake()
    {
        director = new AdaptiveWaveDirector(seed >= 0 ? (int?)seed : null);
    }

    public void BeginLevel()
    {
        if (director == null) Awake();
        director.Reset(!keepLearningAcrossLevels);
        kindCache.Clear();
        pending = null;
        ValidateLevel();
    }

    public void Tick(float elapsedTime, DefenseSnapshot snapshot)
    {
        if (level == null) return;
        director.UseLearning = useLearning;
        director.UseRules = useRules;
        director.Update(elapsedTime, level.GetLevelProgression(elapsedTime), snapshot);
    }

    /// <summary>Call whenever an enemy deals damage. ticket = SpawnModifiers.Ticket from RollModifiers.</summary>
    public void ReportDamage(int ticket, float amount)
    {
        if (director != null && ticket != 0) director.ReportDamage(ticket, amount);
    }

    /// <summary>Call once when an enemy dies or despawns.</summary>
    public void ReportGone(int ticket)
    {
        if (director != null && ticket != 0) director.ReportGone(ticket);
    }

    /// <summary>
    /// Returns the enemy to spawn now, or null (try again next tick).
    /// The AI decides the mix (designer shares x rules x learning). It picks the next enemy first,
    /// then waits until level.GetRemainingBudget(elapsedTime) can afford its cost, so the total
    /// cost of spawned enemies never exceeds the level's budget curve.
    /// </summary>
    public LevelEnemyEntry PickEntry(float elapsedTime)
    {
        if (level == null || level.enemyDataList == null) return null;

        // Re-decide if we've been saving up for too long (e.g. the budget curve dipped after the boss).
        if (pending != null && elapsedTime - pendingSince > maxWaitForPendingSec)
        {
            pending = null;
        }

        if (pending == null)
        {
            pending = DecideNext(elapsedTime);
            pendingSince = elapsedTime;
            if (pending == null) return null;
        }

        if (!level.CanAfford(pending, elapsedTime)) return null; // keep saving

        LevelEnemyEntry result = pending;
        pending = null;
        if (autoCountSpawns) level.RecordSpawn(result);
        return result;
    }

    private LevelEnemyEntry DecideNext(float elapsedTime)
    {
        level.SetEnemyDataSpawnProbability(elapsedTime); // base probabilities from the designer's shares

        // Anything that can ever fit in the level's budget is allowed.
        float ceiling = Mathf.Max(level.maxDifficulty, level.GetTotalBudget(elapsedTime)) - level.SpentBudget;
        if (ceiling <= 0f) return null;

        candidates.Clear();
        foreach (LevelEnemyEntry entry in level.enemyDataList)
        {
            if (entry == null || entry.enemy == null) continue;
            if (excludeLevelBossFromRoster && entry.isLevelBoss) continue;

            var c = new AdaptiveWaveDirector.Candidate();
            c.Kind = KindOf(entry.enemy);
            c.Cost = LevelData.EffectiveCost(entry.enemy);
            // Designer shares are shares of COST, so the chance of picking one enemy is share / cost.
            c.BaseProbability = entry.SpawnProbability / c.Cost;
            c.Tag = entry;
            candidates.Add(c);
        }

        AdaptiveWaveDirector.Candidate picked = director.PickEnemy(candidates, ceiling);
        return picked != null ? (LevelEnemyEntry)picked.Tag : null;
    }

    public SpawnModifiers RollModifiers(LevelEnemyEntry entry, DefenseSnapshot snapshot)
    {
        if (entry == null || entry.enemy == null) return director.RollModifiers(EnemyKind.BasicGoose1, snapshot);
        return director.RollModifiers(KindOf(entry.enemy), LevelData.EffectiveCost(entry.enemy), snapshot);
    }

    /// <summary>True once, 5 seconds before the halfway boss.</summary>
    public bool ShouldShowBossWarning(float previousTime, float time)
    {
        if (level == null) return false;
        return director.ShouldShowBossWarning(previousTime, time, level.maxTime / 2f);
    }

    /// <summary>Warns once per level about settings that would make spawning silently break.</summary>
    private void ValidateLevel()
    {
        if (level == null)
        {
            Debug.LogWarning("[WaveDirector] No LevelData assigned.");
            return;
        }

        string n = level.name;
        if (level.maxTime <= 0f)
            Debug.LogWarning("[WaveDirector] " + n + ": maxTime is " + level.maxTime + ". Set the level length in seconds.");
        if (level.maxDifficulty <= 0 && level.startingDifficulty <= 0)
            Debug.LogWarning("[WaveDirector] " + n + ": difficulty is 0, so no enemies can spawn. Set startingDifficulty / maxDifficulty.");
        if (level.postBossDifficulty < level.maxDifficulty * 0.5f)
            Debug.LogWarning("[WaveDirector] " + n + ": postBossDifficulty (" + level.postBossDifficulty + ") is below maxDifficulty/2 (" + (level.maxDifficulty * 0.5f) +
                             "). Because the budget is cumulative, NO enemies will spawn after the boss until the curve climbs back past what was already spent.");

        if (level.enemyDataList == null || level.enemyDataList.Count == 0)
        {
            Debug.LogWarning("[WaveDirector] " + n + ": enemy roster is empty.");
            return;
        }

        foreach (LevelEnemyEntry e in level.enemyDataList)
        {
            if (e == null || e.enemy == null)
            {
                Debug.LogWarning("[WaveDirector] " + n + ": a roster entry has no EnemyData assigned.");
                continue;
            }
            if (e.enemy.cost <= 0)
                Debug.LogWarning("[WaveDirector] " + n + ": '" + e.enemy.name + "' has cost " + e.enemy.cost + ". Treated as 1; set a real cost (see README).");
            KindOf(e.enemy); // logs a warning if the name doesn't match an enemy kind
        }
    }

    /// <summary>Resolves EnemyKind from the EnemyData asset name.</summary>
    public EnemyKind KindOf(EnemyData data)
    {
        EnemyKind kind;
        if (kindCache.TryGetValue(data, out kind)) return kind;

        string key = data.name.Replace(" ", "").Replace("-", "").Replace("_", "");
        if (!Enum.TryParse(key, true, out kind))
        {
            Debug.LogWarning("[WaveDirector] Unknown enemy asset name '" + data.name +
                             "'. Rename it to match EnemyAI.EnemyKind (e.g. 'Basic Goose 1').");
            kind = EnemyKind.BasicGoose1;
        }

        kindCache[data] = kind;
        return kind;
    }
}
