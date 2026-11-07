// Enemy AI (Wave Director) - Unity glue.
//
// Connects AdaptiveWaveDirector to the team's LevelData. No changes to EnemyData needed:
// the enemy kind is read from the EnemyData ASSET NAME, ignoring spaces, dashes and case.
//   e.g. an asset named "Basic Goose 1" or "BasicGoose1" -> EnemyKind.BasicGoose1
// Unknown names are logged once and treated as BasicGoose1 (no adaptive bonus).
//
// Usage from the spawner:
//   bridge.BeginLevel();                                  // level start (after level.Init())
//   bridge.Tick(elapsedTime, snapshot);                   // every frame or spawn tick
//   level.SetEnemyDataSpawnProbability(elapsedTime);      // team's existing call
//   LevelEnemyEntry entry = bridge.PickEntry();           // instead of rolling SpawnProbability yourself
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
// PickEntry() increments entry.SpawnedCount for you (turn off with autoCountSpawns).

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

    [Tooltip("If true, PickEntry() does entry.SpawnedCount++ on the picked entry. " +
             "Set false only if the spawner already increments it.")]
    public bool autoCountSpawns = true;

    [Tooltip("Online learning on top of the rules. Turn off to compare rules-only behavior.")]
    public bool useLearning = true;

    [Tooltip("If true, the AI keeps what it learned about the player when a new level starts.")]
    public bool keepLearningAcrossLevels = true;

    private AdaptiveWaveDirector director;
    private readonly List<AdaptiveWaveDirector.Candidate> candidates = new List<AdaptiveWaveDirector.Candidate>();
    private readonly Dictionary<EnemyData, EnemyKind> kindCache = new Dictionary<EnemyData, EnemyKind>();

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
    }

    public void Tick(float elapsedTime, DefenseSnapshot snapshot)
    {
        if (level == null) return;
        director.UseLearning = useLearning;
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
    /// Picks the next enemy using LevelData's SpawnProbability x adaptive multipliers.
    /// Returns null when no entry has spawns left (LevelData's counts are all used up).
    /// </summary>
    public LevelEnemyEntry PickEntry()
    {
        if (level == null || level.enemyDataList == null) return null;

        candidates.Clear();
        foreach (LevelEnemyEntry entry in level.enemyDataList)
        {
            if (entry == null || entry.enemy == null) continue;
            if (excludeLevelBossFromRoster && entry.isLevelBoss) continue;

            var c = new AdaptiveWaveDirector.Candidate();
            c.Kind = KindOf(entry.enemy);
            c.BaseProbability = entry.SpawnProbability;
            c.Cost = entry.enemy.cost;
            c.Tag = entry;
            candidates.Add(c);
        }

        // LevelData's budget is cumulative (SpawnedCount), so no extra budget limit here.
        AdaptiveWaveDirector.Candidate picked = director.PickEnemy(candidates, float.MaxValue);
        if (picked == null) return null;

        var result = (LevelEnemyEntry)picked.Tag;
        if (autoCountSpawns) result.SpawnedCount++;
        return result;
    }

    public SpawnModifiers RollModifiers(LevelEnemyEntry entry, DefenseSnapshot snapshot)
    {
        if (entry == null || entry.enemy == null) return director.RollModifiers(EnemyKind.BasicGoose1, snapshot);
        return director.RollModifiers(KindOf(entry.enemy), Mathf.Max(1, entry.enemy.cost), snapshot);
    }

    /// <summary>True once, 5 seconds before the halfway boss.</summary>
    public bool ShouldShowBossWarning(float previousTime, float time)
    {
        if (level == null) return false;
        return director.ShouldShowBossWarning(previousTime, time, level.maxTime / 2f);
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
