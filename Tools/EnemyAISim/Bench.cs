using System;
using System.Collections.Generic;
using System.Linq;
using EnemyAI;

enum Fam { Goose, Stealth, Skunk, Deer }
enum Variant { Random, DesignerOnly, RulesOnly, LearningOnly, RulesPlusLearning, Oracle }

class Result
{
    public double WeakShare;      // weak family's share of spent cost (after 20%)
    public double Efficiency;     // damage per spent cost (after 20%)
    public double[] FamShare = new double[4];
    public int Spawns;
    public bool BudgetViolated;
}

static class Bench
{
    static Fam FamOf(EnemyKind k)
    {
        if (k == EnemyKind.StealthGoose) return Fam.Stealth;
        if (EnemyCatalog.IsGoose(k)) return Fam.Goose;
        if (EnemyCatalog.IsSkunk(k)) return Fam.Skunk;
        return Fam.Deer;
    }

    static LevelData MakeLevel(float maxTime, int maxDiff, bool uniformShares)
    {
        var lv = new LevelData { maxTime = maxTime, startingDifficulty = 20, postBossDifficulty = maxDiff * 3 / 10, maxDifficulty = maxDiff, enemyDataList = new List<LevelEnemyEntry>() };
        foreach (var kv in EnemyCatalog.DefaultWeight)
        {
            bool deer = EnemyCatalog.IsDeer(kv.Key);
            float s0 = uniformShares ? 1f : (deer ? 0.02f : 0.1f);
            float s1 = uniformShares ? 1f : (deer ? 0.1f : 0.08f);
            lv.enemyDataList.Add(new LevelEnemyEntry { enemy = new EnemyData { name = kv.Key.ToString(), cost = kv.Value }, startingBudget = s0, endingBudget = s1 });
        }
        lv.enemyDataList.Add(new LevelEnemyEntry { enemy = new EnemyData { name = "Mother Goose", cost = 40, isBoss = true }, isLevelBoss = true, startingBudget = 1, endingBudget = 1 });
        return lv;
    }

    public static DefenseSnapshot Defense(string kind)
    {
        var s = new DefenseSnapshot { FuelPerSecond = 1 };
        TowerKind[] t;
        switch (kind)
        {
            case "aoe": t = new[] { TowerKind.Radar, TowerKind.Bomb, TowerKind.TeslaCoil, TowerKind.Bomb, TowerKind.Laser, TowerKind.TeslaCoil }; break; // rules say: send deer
            case "noradar": t = new[] { TowerKind.Laser, TowerKind.Sniper, TowerKind.Laser, TowerKind.PestTrap, TowerKind.Bomb, TowerKind.Laser }; break; // rules say: send stealth
            default: t = new[] { TowerKind.Radar, TowerKind.Laser, TowerKind.Sniper, TowerKind.Laser, TowerKind.PestTrap, TowerKind.Laser }; break; // neutral
        }
        for (int i = 0; i < t.Length; i++) s.Towers.Add(new TowerInfo { Kind = t[i], AngleDeg = i * 60 });
        return s;
    }

    /// <summary>Runs one level. mult(t, family) = damage per cost the player lets through.</summary>
    public static Result Run(Variant v, int seed, Func<float, Fam, double> mult, string defense,
                             float maxTime = 180, int maxDiff = 400, Action<float, WaveDirectorBridge, LevelData, EnemyKind?> onTick = null,
                             EnemyLearnerSettings ls = null, WaveDirectorBridge reuse = null)
    {
        var lv = MakeLevel(maxTime, maxDiff, v == Variant.Random);
        lv.Init();
        var env = new Random(seed * 7919 + 13);
        var snap = Defense(defense);
        var res = new Result();
        double spent = 0, dmgSum = 0;
        var famCost = new double[4];

        WaveDirectorBridge b = reuse;
        if (b == null)
        {
            b = new WaveDirectorBridge { seed = seed };
            if (ls != null)
            {
                b.BeginLevel(); // creates the director
                b.Director.Learner.Settings.DecayPerStep = ls.DecayPerStep;
            }
        }
        b.level = lv;
        b.useRules = v == Variant.RulesOnly || v == Variant.RulesPlusLearning;
        b.useLearning = v == Variant.LearningOnly || v == Variant.RulesPlusLearning;
        b.BeginLevel();

        // Oracle state
        LevelEnemyEntry oraclePending = null;
        var orng = new Random(seed + 1);

        for (float t = 0; t <= maxTime; t += 0.5f)
        {
            LevelEnemyEntry e;
            if (v == Variant.Oracle)
            {
                lv.SetEnemyDataSpawnProbability(t);
                if (oraclePending == null)
                {
                    var best = lv.enemyDataList.Where(x => !x.isLevelBoss).ToList();
                    double m = best.Max(x => mult(t, FamOf(b.KindOf(x.enemy))));
                    var top = best.Where(x => mult(t, FamOf(b.KindOf(x.enemy))) >= m - 1e-9).ToList();
                    oraclePending = top[orng.Next(top.Count)];
                }
                e = null;
                if (lv.CanAfford(oraclePending, t)) { e = oraclePending; oraclePending = null; lv.RecordSpawn(e); }
            }
            else
            {
                b.Tick(t, snap);
                e = b.PickEntry(t);
            }

            EnemyKind? spawnedKind = null;
            if (e != null)
            {
                if (lv.SpentBudget > lv.GetTotalBudget(t) + 1e-3) res.BudgetViolated = true;
                var kind = b.KindOf(e.enemy);
                spawnedKind = kind;
                var f = FamOf(kind);
                var mods = b.RollModifiers(e, snap);
                double dmg = e.enemy.cost * mult(t, f) * (0.5 + env.NextDouble());
                b.ReportDamage(mods.Ticket, (float)dmg);
                b.ReportGone(mods.Ticket);
                res.Spawns++;
                if (t >= maxTime * 0.2f)
                {
                    spent += e.enemy.cost; dmgSum += dmg; famCost[(int)f] += e.enemy.cost;
                }
            }
            onTick?.Invoke(t, b, lv, spawnedKind);
        }

        for (int i = 0; i < 4; i++) res.FamShare[i] = spent > 0 ? famCost[i] / spent : 0;
        res.Efficiency = spent > 0 ? dmgSum / spent : 0;
        return res;
    }

    public static string Ci(IEnumerable<double> xs, string fmt = "F1", double scale = 1)
    {
        var a = xs.Select(x => x * scale).ToArray();
        double mean = a.Average();
        double sd = Math.Sqrt(a.Select(x => (x - mean) * (x - mean)).Sum() / (a.Length - 1));
        double ci = 1.96 * sd / Math.Sqrt(a.Length);
        return mean.ToString(fmt) + " ± " + ci.ToString(fmt);
    }
}
