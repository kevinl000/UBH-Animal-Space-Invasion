using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnemyAI;

static class Edge
{
    static int pass, fail;

    static void Check(string name, Func<string> body)
    {
        var sw = Stopwatch.StartNew();
        string detail;
        bool ok;
        try { detail = body(); ok = !detail.StartsWith("FAIL"); }
        catch (Exception ex) { detail = "EXCEPTION " + ex.GetType().Name + ": " + ex.Message; ok = false; }
        if (sw.ElapsedMilliseconds > 2000) { ok = false; detail += " (too slow: " + sw.ElapsedMilliseconds + "ms, possible infinite loop)"; }
        if (ok) pass++; else fail++;
        Console.WriteLine((ok ? "PASS " : "FAIL ") + name + " -> " + detail);
    }

    static LevelData Level(int start, int max, params int[] costs)
    {
        var lv = new LevelData { maxTime = 60, startingDifficulty = start, postBossDifficulty = max / 2, maxDifficulty = max, enemyDataList = new List<LevelEnemyEntry>() };
        var kinds = EnemyCatalog.DefaultWeight.Keys.ToArray();
        for (int i = 0; i < costs.Length; i++)
            lv.enemyDataList.Add(new LevelEnemyEntry { enemy = new EnemyData { name = kinds[i % kinds.Length].ToString(), cost = costs[i] }, startingBudget = 1, endingBudget = 1 });
        return lv;
    }

    static string Loop(LevelData lv, DefenseSnapshot snap, float seconds = 60)
    {
        lv.Init();
        var b = new WaveDirectorBridge { level = lv, seed = 3 };
        b.BeginLevel();
        int spawns = 0;
        for (float t = 0; t <= seconds; t += 0.5f)
        {
            b.Tick(t, snap);
            var e = b.PickEntry(t);
            if (lv.SpentBudget > lv.GetTotalBudget(t) + 1e-3 && e != null) return "FAIL budget exceeded at spawn";
            if (e == null) continue;
            var m = b.RollModifiers(e, snap);
            if (float.IsNaN(m.SpawnAngleDeg) || m.SpawnAngleDeg < 0 || m.SpawnAngleDeg >= 360) return "FAIL bad angle " + m.SpawnAngleDeg;
            b.ReportDamage(m.Ticket, float.NaN);   // garbage damage must not poison learning
            b.ReportDamage(m.Ticket, -5f);
            b.ReportDamage(m.Ticket, 3f);
            b.ReportGone(m.Ticket);
            b.ReportGone(m.Ticket);               // reported twice
            spawns++;
        }
        foreach (EnemyKind k in Enum.GetValues(typeof(EnemyKind)))
            if (float.IsNaN(b.Director.Learner.MeanReward(k))) return "FAIL learner NaN for " + k;
        return spawns + " spawns, spent " + lv.SpentBudget + "/" + lv.GetTotalBudget(seconds);
    }

    public static void Main3()
    {
        Console.WriteLine("=== Test 4: edge cases");
        var neutral = Bench.Defense("neutral");

        Check("budget below the cheapest enemy (budget 2, cheapest 3)", () => { var r = Loop(Level(1, 2, 3, 8), neutral); return r.StartsWith("0 spawns") ? r : "FAIL expected 0 spawns: " + r; });
        Check("budget 0", () => { var r = Loop(Level(0, 0, 3, 8), neutral); return r.StartsWith("0 spawns") ? r : "FAIL " + r; });
        Check("single enemy type", () => Loop(Level(20, 200, 5), neutral));
        Check("enemy more expensive than the whole level (cost 500, budget 200) + one cheap enemy", () => Loop(Level(20, 200, 500, 3), neutral));
        Check("empty roster", () => Loop(new LevelData { maxTime = 60, startingDifficulty = 20, maxDifficulty = 100, enemyDataList = new List<LevelEnemyEntry>() }, neutral));
        Check("null roster", () => Loop(new LevelData { maxTime = 60, startingDifficulty = 20, maxDifficulty = 100, enemyDataList = null }, neutral));
        Check("roster entry with no EnemyData", () => { var lv = Level(20, 100, 3); lv.enemyDataList.Add(new LevelEnemyEntry { enemy = null }); return Loop(lv, neutral); });
        Check("negative shares", () => { var lv = Level(20, 100, 3, 8); lv.enemyDataList[0].startingBudget = -5; lv.enemyDataList[0].endingBudget = -5; return Loop(lv, neutral); });
        Check("negative cost", () => Loop(Level(20, 100, -4, 8), neutral));
        Check("no towers", () => Loop(Level(20, 200, 3, 8, 20), new DefenseSnapshot()));
        Check("null snapshot", () => Loop(Level(20, 200, 3, 8, 20), null));
        Check("snapshot with null towers list and null entries", () => { var s = new DefenseSnapshot { Towers = null }; var r1 = Loop(Level(20, 100, 3), s); var s2 = new DefenseSnapshot(); s2.Towers.Add(null); return r1 + " / " + Loop(Level(20, 100, 3), s2); });
        Check("all 360 degrees covered (no weak direction)", () =>
        {
            var s = new DefenseSnapshot();
            for (int i = 0; i < 12; i++) s.Towers.Add(new TowerInfo { Kind = TowerKind.Laser, AngleDeg = i * 30 });
            return Loop(Level(20, 200, 3, 8), s);
        });
        Check("FOV wraps 359 -> 0 degrees", () =>
        {
            var d = new AdaptiveWaveDirector(1);
            var s = new DefenseSnapshot(); s.Towers.Add(new TowerInfo { Kind = TowerKind.Laser, AngleDeg = 359 });
            int c1 = d.Coverage(s, 1f), c2 = d.Coverage(s, 300f), c3 = d.Coverage(s, 58f), c4 = d.Coverage(s, 62f);
            float dist = AdaptiveWaveDirector.AngleDistance(359, 1);
            bool ok = c1 == 1 && c2 == 1 && c3 == 1 && c4 == 0 && Math.Abs(dist - 2) < 1e-4;
            return (ok ? "" : "FAIL ") + "coverage at 1/300/58/62 deg = " + c1 + "/" + c2 + "/" + c3 + "/" + c4 + ", distance(359,1)=" + dist;
        });
        Check("angles outside 0-360 (-30, 720)", () =>
        {
            var d = new AdaptiveWaveDirector(1);
            var s = new DefenseSnapshot(); s.Towers.Add(new TowerInfo { Kind = TowerKind.Laser, AngleDeg = -30 }); s.Towers.Add(new TowerInfo { Kind = TowerKind.Laser, AngleDeg = 720 });
            int c = d.Coverage(s, 330f);
            float a = d.ChooseSpawnAngle(s);
            return (c == 2 && a >= 0 && a < 360 ? "" : "FAIL ") + "coverage at 330 (expected 2: -30 is 330, 720 is 0 and 0 also sees 330) = " + c + ", chosen angle " + a;
        });
        Check("PickEnemy with all-zero probabilities returns null", () =>
        {
            var d = new AdaptiveWaveDirector(1);
            var c = new List<AdaptiveWaveDirector.Candidate> { new AdaptiveWaveDirector.Candidate { Kind = EnemyKind.BasicGoose1, BaseProbability = 0, Cost = 3 } };
            return d.PickEnemy(c, 100) == null ? "null" : "FAIL picked something";
        });
        Check("PickEnemy with NaN probability", () =>
        {
            var d = new AdaptiveWaveDirector(1);
            var c = new List<AdaptiveWaveDirector.Candidate> {
                new AdaptiveWaveDirector.Candidate { Kind = EnemyKind.BasicGoose1, BaseProbability = float.NaN, Cost = 3 },
                new AdaptiveWaveDirector.Candidate { Kind = EnemyKind.BasicDeer, BaseProbability = 1, Cost = 20 } };
            int deer = 0; for (int i = 0; i < 200; i++) { var p = d.PickEnemy(c, 100); if (p != null && p.Kind == EnemyKind.BasicDeer) deer++; }
            return (deer == 200 ? "" : "FAIL ") + "deer picked " + deer + "/200";
        });
        Check("learner after 10,000 decays with no data (no NaN, still samples)", () =>
        {
            var l = new EnemyLearner(new Random(1));
            for (int i = 0; i < 10000; i++) l.Decay();
            float f = l.SampleFactor(EnemyKind.BasicDeer);
            return (float.IsNaN(f) ? "FAIL " : "") + "factor " + f;
        });
        Check("learner with 100,000 results on one family", () =>
        {
            var l = new EnemyLearner(new Random(1));
            for (int i = 0; i < 100000; i++) { int t = l.Register(EnemyKind.BasicDeer, 20); l.ReportDamage(t, 1000); l.ReportGone(t); }
            float f = l.SampleFactor(EnemyKind.BasicDeer), m = l.MeanReward(EnemyKind.BasicDeer);
            return (float.IsNaN(f) || f > 2.0001f ? "FAIL " : "") + "factor " + f + " (cap 2), mean " + m;
        });
        Check("tick with time going backwards / huge time", () =>
        {
            var lv = Level(20, 200, 3, 8); lv.Init();
            var b = new WaveDirectorBridge { level = lv, seed = 1 }; b.BeginLevel();
            foreach (float t in new[] { 10f, 5f, 0f, 1e9f, float.MaxValue }) { b.Tick(t, neutral); b.PickEntry(t); }
            return "ok, spent " + lv.SpentBudget;
        });

        Console.WriteLine("Edge cases: " + pass + " passed, " + fail + " failed");
        Console.WriteLine();
    }
}
