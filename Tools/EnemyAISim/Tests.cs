using System;
using System.Collections.Generic;
using System.Linq;
using EnemyAI;

static class Tests
{
    const int N = 200;

    static void Main(string[] args)
    {
        string which = args.Length > 0 ? args[0] : "all";
        if (which == "all" || which == "1") Baselines();
        if (which == "all" || which == "2") StrategySwitch();
        if (which == "all" || which == "5") WrongPrior();
        if (which == "all" || which == "6") NoWeakness();
        if (which == "all" || which == "3") Integ.Main2();
        if (which == "all" || which == "4") Edge.Main3();
    }

    // ---------------------------------------------------------------- Test 1
    static void Baselines()
    {
        Console.WriteLine("=== Test 1: baselines (player weak to deer, AoE-heavy defense so the rules point to deer), N=" + N);
        Func<float, Fam, double> weakDeer = (t, f) => f == Fam.Deer ? 2.0 : 0.2;
        var rows = new List<Tuple<Variant, List<Result>>>();
        foreach (Variant v in Enum.GetValues(typeof(Variant)))
        {
            var rs = new List<Result>();
            for (int s = 0; s < N; s++) rs.Add(Bench.Run(v, s, weakDeer, "aoe"));
            rows.Add(Tuple.Create(v, rs));
        }
        double oracleEff = rows.First(r => r.Item1 == Variant.Oracle).Item2.Average(r => r.Efficiency);
        double randomEff = rows.First(r => r.Item1 == Variant.Random).Item2.Average(r => r.Efficiency);
        Console.WriteLine("| Variant | Deer share of cost (%) | Damage per cost | % of oracle | Gap closed vs random (%) | Budget violations |");
        Console.WriteLine("|---|---|---|---|---|---|");
        foreach (var r in rows)
        {
            double eff = r.Item2.Average(x => x.Efficiency);
            Console.WriteLine("| " + r.Item1 + " | " + Bench.Ci(r.Item2.Select(x => x.WeakShareOf(Fam.Deer)), "F1", 100)
                + " | " + Bench.Ci(r.Item2.Select(x => x.Efficiency), "F3")
                + " | " + Bench.Ci(r.Item2.Select(x => x.Efficiency / oracleEff), "F1", 100)
                + " | " + (100 * (eff - randomEff) / (oracleEff - randomEff)).ToString("F1")
                + " | " + r.Item2.Count(x => x.BudgetViolated) + "/" + N + " |");
        }
        Console.WriteLine();
    }

    // ---------------------------------------------------------------- Test 2
    static void StrategySwitch()
    {
        Console.WriteLine("=== Test 2: weakness switches skunk -> deer at t=90s (360s level, boss at 180s, neutral defense, rules+learning), N=" + N);
        Console.WriteLine("    'flip' = learner believes deer > skunks. One 'wave' = one 15s re-evaluation step.");
        Console.WriteLine("| Decay per 15s | Flip after switch (s) | Never | Deer share 0-30s after | 30-60s | 60-90s | Reference: deer weak from start, same 60-90s window | Baseline test dmg/cost (% oracle) |");
        Console.WriteLine("|---|---|---|---|---|---|---|---|");
        foreach (float decay in new[] { 0.95f, 0.85f, 0.7f })
        {
            var flips = new List<double>(); int never = 0;
            var w = new List<double>[3] { new List<double>(), new List<double>(), new List<double>() };
            var refw = new List<double>(); var baseEff = new List<double>();
            for (int s = 0; s < N; s++)
            {
                double? flip = null;
                double[] dc = new double[3], tc = new double[3];
                Func<float, Fam, double> m = (t, f) => t < 90 ? (f == Fam.Skunk ? 2.0 : 0.2) : (f == Fam.Deer ? 2.0 : 0.2);
                Bench.Run(Variant.RulesPlusLearning, s, m, "neutral", 360, 800, (t, b, lv, k) =>
                {
                    if (t >= 90 && flip == null && b.Director.Learner.MeanReward(EnemyKind.BasicDeer) > b.Director.Learner.MeanReward(EnemyKind.BasicSkunk1))
                        flip = t - 90;
                    if (k == null || t < 90 || t >= 180) return;
                    int i = (int)((t - 90) / 30);
                    double c = EnemyCatalog.DefaultWeight[k.Value];
                    tc[i] += c; if (EnemyCatalog.IsDeer(k.Value)) dc[i] += c;
                }, new EnemyLearnerSettings { DecayPerStep = decay });
                if (flip.HasValue) flips.Add(flip.Value); else never++;
                for (int i = 0; i < 3; i++) w[i].Add(dc[i] / Math.Max(1, tc[i]));

                double rd = 0, rt = 0;
                Bench.Run(Variant.RulesPlusLearning, s, (t, f) => f == Fam.Deer ? 2.0 : 0.2, "neutral", 360, 800, (t, b, lv, k) =>
                {
                    if (k == null || t < 150 || t >= 180) return;
                    double c = EnemyCatalog.DefaultWeight[k.Value]; rt += c; if (EnemyCatalog.IsDeer(k.Value)) rd += c;
                }, new EnemyLearnerSettings { DecayPerStep = decay });
                refw.Add(rd / Math.Max(1, rt));

                baseEff.Add(Bench.Run(Variant.RulesPlusLearning, s, (t, f) => f == Fam.Deer ? 2.0 : 0.2, "aoe", 180, 400, null, new EnemyLearnerSettings { DecayPerStep = decay }).Efficiency / 1.998);
            }
            Console.WriteLine("| " + decay + " | " + Bench.Ci(flips, "F0") + " (median " + Median(flips).ToString("F0") + ")"
                + " | " + never + "/" + N + " | " + Bench.Ci(w[0], "F1", 100) + "% | " + Bench.Ci(w[1], "F1", 100) + "% | " + Bench.Ci(w[2], "F1", 100) + "% | "
                + Bench.Ci(refw, "F1", 100) + "% | " + Bench.Ci(baseEff, "F1", 100) + "% |");
        }
        Console.WriteLine();
    }

    // ---------------------------------------------------------------- Test 5
    static void WrongPrior()
    {
        Console.WriteLine("=== Test 5: rules are wrong (no radar -> rules boost stealth geese), real weakness = deer, N=" + N);
        Func<float, Fam, double> weakDeer = (t, f) => f == Fam.Deer ? 2.0 : 0.2;
        var flips = new List<double>(); int never = 0;
        var rl = new List<Result>(); var ro = new List<Result>();
        for (int s = 0; s < N; s++)
        {
            double? flip = null;
            rl.Add(Bench.Run(Variant.RulesPlusLearning, s, weakDeer, "noradar", 180, 400, (t, b, lv, k) =>
            {
                if (t >= 36 && flip == null && b.Director.Learner.SampleCount(EnemyKind.BasicDeer) > 0
                    && b.Director.Learner.MeanReward(EnemyKind.BasicDeer) > b.Director.Learner.MeanReward(EnemyKind.StealthGoose))
                    flip = t - 36;
            }));
            if (flip.HasValue) flips.Add(flip.Value); else never++;
            ro.Add(Bench.Run(Variant.RulesOnly, s, weakDeer, "noradar"));
        }
        Console.WriteLine("| Variant | Deer share (%) | Stealth share (%) | Damage per cost |");
        Console.WriteLine("|---|---|---|---|");
        Console.WriteLine("| RulesOnly | " + Bench.Ci(ro.Select(x => x.WeakShareOf(Fam.Deer)), "F1", 100) + " | " + Bench.Ci(ro.Select(x => x.WeakShareOf(Fam.Stealth)), "F1", 100) + " | " + Bench.Ci(ro.Select(x => x.Efficiency), "F3") + " |");
        Console.WriteLine("| RulesPlusLearning | " + Bench.Ci(rl.Select(x => x.WeakShareOf(Fam.Deer)), "F1", 100) + " | " + Bench.Ci(rl.Select(x => x.WeakShareOf(Fam.Stealth)), "F1", 100) + " | " + Bench.Ci(rl.Select(x => x.Efficiency), "F3") + " |");
        Console.WriteLine("Time from adaptation start (36s) until learner ranks deer above stealth: " + (flips.Count > 1 ? Bench.Ci(flips, "F0") + "s, median " + Median(flips).ToString("F0") + "s" : "-") + "; never: " + never + "/" + N);
        Console.WriteLine();
    }

    // ---------------------------------------------------------------- Test 6
    static void NoWeakness()
    {
        Console.WriteLine("=== Test 6: no weakness (every family deals 0.7x cost), neutral defense, N=" + N);
        Func<float, Fam, double> flat = (t, f) => 0.7;
        Console.WriteLine("| Variant | Normalized entropy of family mix (1 = perfectly even) | Largest family share (%) |");
        Console.WriteLine("|---|---|---|");
        foreach (var v in new[] { Variant.DesignerOnly, Variant.RulesPlusLearning })
        {
            var rs = new List<Result>();
            for (int s = 0; s < N; s++) rs.Add(Bench.Run(v, s, flat, "neutral"));
            Console.WriteLine("| " + v + " | " + Bench.Ci(rs.Select(r => Entropy(r.FamShare)), "F3") + " | " + Bench.Ci(rs.Select(r => r.FamShare.Max()), "F1", 100) + " |");
        }
        Console.WriteLine();
    }

    static double Entropy(double[] p)
    {
        double h = 0;
        foreach (double x in p) if (x > 0) h -= x * Math.Log(x);
        return h / Math.Log(p.Length);
    }

    static double Median(List<double> xs)
    {
        var a = xs.OrderBy(x => x).ToArray();
        return a.Length % 2 == 1 ? a[a.Length / 2] : (a[a.Length / 2 - 1] + a[a.Length / 2]) / 2;
    }

    public static double WeakShareOf(this Result r, Fam f) { return r.FamShare[(int)f]; }
}
