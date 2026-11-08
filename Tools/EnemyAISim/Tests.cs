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
        // v1.3: raw per-seed experiments for paired statistics (report13.py)
        if (new[] { "v13", "abstract", "combat", "r4a", "r4b", "r1" }.Contains(which)) { V13.Main13(which); return; }
        if (which == "r4d") { V13.MainCap(); return; }
        if (which == "switch") { V13.MainSwitch(); return; }
        if (which == "all" || which == "1") Baselines();
        if (which == "all" || which == "2") StrategySwitch();
        if (which == "all" || which == "5") WrongPrior();
        if (which == "all" || which == "6") NoWeakness();
        if (which == "all" || which == "7") CombatTest();
        if (which == "all" || which == "3") Integ.Main2();
        if (which == "all" || which == "4") Edge.Main3();
        if (which == "all")
        {
            Json.Add("\"edge_cases\":{\"passed\":" + Edge.Passed + ",\"failed\":" + Edge.Failed + ",\"cases\":[" + string.Join(",", Edge.Cases.Select(c => "{\"name\":" + Str(c.Item1) + ",\"ok\":" + (c.Item2 ? "true" : "false") + "}")) + "]}");
            Json.Add("\"integration\":[" + string.Join(",", Integ.Lines.Select(Str)) + "]");
            WriteJson("results.json");
        }
    }

    // ---------------------------------------------------------------- JSON output (read by report.py)
    static readonly List<string> Json = new List<string>();

    static string Num(double x) { return double.IsNaN(x) || double.IsInfinity(x) ? "null" : x.ToString("R", System.Globalization.CultureInfo.InvariantCulture); }

    static string Stat(IEnumerable<double> xs, double scale = 1)
    {
        var a = xs.Select(x => x * scale).ToArray();
        if (a.Length == 0) return "{\"mean\":null,\"ci\":null,\"n\":0}";
        double mean = a.Average();
        double sd = a.Length > 1 ? Math.Sqrt(a.Select(x => (x - mean) * (x - mean)).Sum() / (a.Length - 1)) : 0;
        return "{\"mean\":" + Num(mean) + ",\"ci\":" + Num(1.96 * sd / Math.Sqrt(a.Length)) + ",\"n\":" + a.Length + "}";
    }

    static string Str(string x) { return "\"" + x.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }

    static void WriteJson(string path)
    {
        string body = "{\"runs_per_test\":" + N + ",\"generated_utc\":" + Str(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")) + "," + string.Join(",", Json) + "}";
        System.IO.File.WriteAllText(path, body);
        Console.WriteLine("Wrote " + path);
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
        Json.Add("\"ablation\":[" + string.Join(",", rows.Select(r =>
            "{\"variant\":" + Str(r.Item1.ToString())
            + ",\"deer_share_pct\":" + Stat(r.Item2.Select(x => x.WeakShareOf(Fam.Deer)), 100)
            + ",\"pct_of_oracle\":" + Stat(r.Item2.Select(x => x.Efficiency / oracleEff), 100)
            + ",\"gap_closed_pct\":" + Num(100 * (r.Item2.Average(x => x.Efficiency) - randomEff) / (oracleEff - randomEff))
            + ",\"budget_violations\":" + r.Item2.Count(x => x.BudgetViolated) + "}")) + "]");
        Console.WriteLine();
    }

    // ---------------------------------------------------------------- Test 2
    static void StrategySwitch()
    {
        Console.WriteLine("=== Test 2: weakness switches skunk -> deer at t=90s (360s level, boss at 180s, neutral defense, rules+learning), N=" + N);
        Console.WriteLine("    'flip' = learner believes deer > skunks. One 'wave' = one 15s re-evaluation step.");
        Console.WriteLine("| Decay per 15s | Flip after switch (s) | Never | Deer share 0-30s after | 30-60s | 60-90s | Reference: deer weak from start, same 60-90s window | Baseline test dmg/cost (% oracle) |");
        Console.WriteLine("|---|---|---|---|---|---|---|---|");
        var switchRows = new List<string>();
        foreach (float decay in new[] { 0.95f, 0.85f, 0.7f })
        {
            var flips = new List<double>(); int never = 0;
            var w = new List<double>[3] { new List<double>(), new List<double>(), new List<double>() };
            var refw = new List<double>(); var baseEff = new List<double>();
            const int B = 12; // 15s buckets over 0-180s
            var deerTs = new List<double>[B]; var skunkTs = new List<double>[B];
            for (int i = 0; i < B; i++) { deerTs[i] = new List<double>(); skunkTs[i] = new List<double>(); }
            for (int s = 0; s < N; s++)
            {
                double? flip = null;
                double[] dc = new double[3], tc = new double[3];
                double[] bd = new double[B], bs = new double[B], bt = new double[B];
                Func<float, Fam, double> m = (t, f) => t < 90 ? (f == Fam.Skunk ? 2.0 : 0.2) : (f == Fam.Deer ? 2.0 : 0.2);
                Bench.Run(Variant.RulesPlusLearning, s, m, "neutral", 360, 800, (t, b, lv, k) =>
                {
                    if (t >= 90 && flip == null && b.Director.Learner.MeanReward(EnemyKind.BasicDeer) > b.Director.Learner.MeanReward(EnemyKind.BasicSkunk1))
                        flip = t - 90;
                    if (k == null || t >= 180) return;
                    int bi = (int)(t / 15); double bc = EnemyCatalog.DefaultWeight[k.Value];
                    bt[bi] += bc; if (EnemyCatalog.IsDeer(k.Value)) bd[bi] += bc; if (EnemyCatalog.IsSkunk(k.Value)) bs[bi] += bc;
                    if (t < 90) return;
                    int i = (int)((t - 90) / 30);
                    double c = EnemyCatalog.DefaultWeight[k.Value];
                    tc[i] += c; if (EnemyCatalog.IsDeer(k.Value)) dc[i] += c;
                }, new EnemyLearnerSettings { DecayPerStep = decay });
                if (flip.HasValue) flips.Add(flip.Value); else never++;
                for (int i = 0; i < 3; i++) w[i].Add(dc[i] / Math.Max(1, tc[i]));
                for (int i = 0; i < B; i++) if (bt[i] > 0) { deerTs[i].Add(bd[i] / bt[i]); skunkTs[i].Add(bs[i] / bt[i]); }

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
            switchRows.Add("{\"decay\":" + Num(decay) + ",\"flip_median_s\":" + Num(flips.Count > 0 ? Median(flips) : double.NaN)
                + ",\"flip_s\":" + Stat(flips) + ",\"never\":" + never
                + ",\"deer_share_after_pct\":[" + Stat(w[0], 100) + "," + Stat(w[1], 100) + "," + Stat(w[2], 100) + "]"
                + ",\"reference_pct\":" + Stat(refw, 100) + ",\"steady_pct_of_oracle\":" + Stat(baseEff, 100)
                + ",\"timeseries\":{\"bucket_s\":15,\"deer_pct\":[" + string.Join(",", deerTs.Select(x => Stat(x, 100))) + "],\"skunk_pct\":[" + string.Join(",", skunkTs.Select(x => Stat(x, 100))) + "]}}");
        }
        // Algorithm-only reference: rules, no learning, same switch scenario
        var ro = new List<double>[3] { new List<double>(), new List<double>(), new List<double>() };
        for (int s = 0; s < N; s++)
        {
            double[] dc = new double[3], tc = new double[3];
            Func<float, Fam, double> m = (t, f) => t < 90 ? (f == Fam.Skunk ? 2.0 : 0.2) : (f == Fam.Deer ? 2.0 : 0.2);
            Bench.Run(Variant.RulesOnly, s, m, "neutral", 360, 800, (t, b, lv, k) =>
            {
                if (k == null || t < 90 || t >= 180) return;
                int i = (int)((t - 90) / 30);
                double c = EnemyCatalog.DefaultWeight[k.Value];
                tc[i] += c; if (EnemyCatalog.IsDeer(k.Value)) dc[i] += c;
            });
            for (int i = 0; i < 3; i++) ro[i].Add(dc[i] / Math.Max(1, tc[i]));
        }
        Console.WriteLine("Rules only (no learning), deer share 0-30 / 30-60 / 60-90s after switch: " + Bench.Ci(ro[0], "F1", 100) + "% / " + Bench.Ci(ro[1], "F1", 100) + "% / " + Bench.Ci(ro[2], "F1", 100) + "%");
        Json.Add("\"strategy_switch\":{\"switch_at_s\":90,\"rules_only_deer_share_after_pct\":[" + Stat(ro[0], 100) + "," + Stat(ro[1], 100) + "," + Stat(ro[2], 100) + "],\"rows\":[" + string.Join(",", switchRows) + "]}");
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
        Json.Add("\"wrong_rules\":{\"rules_only_deer_pct\":" + Stat(ro.Select(x => x.WeakShareOf(Fam.Deer)), 100)
            + ",\"rules_learning_deer_pct\":" + Stat(rl.Select(x => x.WeakShareOf(Fam.Deer)), 100)
            + ",\"rules_only_stealth_pct\":" + Stat(ro.Select(x => x.WeakShareOf(Fam.Stealth)), 100)
            + ",\"rules_learning_stealth_pct\":" + Stat(rl.Select(x => x.WeakShareOf(Fam.Stealth)), 100)
            + ",\"override_median_s\":" + Num(flips.Count > 0 ? Median(flips) : double.NaN) + ",\"never\":" + never + "}");
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
        var nw = new List<string>();
        foreach (var v in new[] { Variant.DesignerOnly, Variant.RulesOnly, Variant.RulesPlusLearning })
        {
            var rs = new List<Result>();
            for (int s = 0; s < N; s++) rs.Add(Bench.Run(v, s, flat, "neutral"));
            nw.Add("{\"variant\":" + Str(v.ToString()) + ",\"entropy\":" + Stat(rs.Select(r => Entropy(r.FamShare))) + ",\"largest_share_pct\":" + Stat(rs.Select(r => r.FamShare.Max()), 100) + "}");
            Console.WriteLine("| " + v + " | " + Bench.Ci(rs.Select(r => Entropy(r.FamShare)), "F3") + " | " + Bench.Ci(rs.Select(r => r.FamShare.Max()), "F1", 100) + " |");
        }
        Json.Add("\"no_weakness\":[" + string.Join(",", nw) + "]");
        Console.WriteLine();
    }

    // ---------------------------------------------------------------- Test 7
    static void CombatTest()
    {
        const int NC = 100;
        string[] famName = { "Geese", "Stealth geese", "Skunks", "Deer" };
        Console.WriteLine("=== Test 7: combat model from README stats (weakness comes from the tower loadout, not assumed), N=" + NC);
        Console.WriteLine("    Pressure = damage dealt to the player's towers + planet per spent cost (after 20% of the level). Higher = harder on the player.");
        Console.WriteLine("| Loadout | Random | Designer only | Rules only | Learning only | Rules + learning | Best single family (hindsight) | R+L family mix (G/S/Sk/D %) |");
        Console.WriteLine("|---|---|---|---|---|---|---|---|");
        var rowsJson = new List<string>();
        var variants = new[] { Variant.Random, Variant.DesignerOnly, Variant.RulesOnly, Variant.LearningOnly, Variant.RulesPlusLearning };
        foreach (var lo in Combat.Loadouts.Keys)
        {
            var cells = new List<string>(); var vj = new List<string>();
            List<CombatResult> rl = null;
            foreach (var v in variants)
            {
                var rs = Enumerable.Range(0, NC).Select(s => Combat.Run(v, s, lo)).ToList();
                if (v == Variant.RulesPlusLearning) rl = rs;
                cells.Add(Bench.Ci(rs.Select(r => r.Pressure), "F3"));
                vj.Add("{\"variant\":" + Str(v.ToString()) + ",\"pressure\":" + Stat(rs.Select(r => r.Pressure)) + ",\"planet_damage\":" + Stat(rs.Select(r => r.PlanetDamage)) + ",\"towers_lost\":" + Stat(rs.Select(r => (double)r.TowersLost)) + "}");
            }
            double best = double.MinValue; int bestFam = 0; var famP = new double[4];
            for (int f = 0; f < 4; f++)
            {
                famP[f] = Enumerable.Range(0, NC).Select(s => Combat.Run(Variant.DesignerOnly, s, lo, (Fam)f).Pressure).Average();
                if (famP[f] > best) { best = famP[f]; bestFam = f; }
            }
            var mix = Enumerable.Range(0, 4).Select(f => 100 * rl.Average(r => r.Spent > 0 ? r.FamCost[f] / r.Spent : 0)).ToArray();
            Console.WriteLine("| " + lo + " | " + string.Join(" | ", cells) + " | " + famName[bestFam] + " " + best.ToString("F3") + " | " + string.Join("/", mix.Select(x => x.ToString("F0"))) + " |");
            rowsJson.Add("{\"loadout\":" + Str(lo) + ",\"variants\":[" + string.Join(",", vj) + "],\"family_only_pressure\":[" + string.Join(",", famP.Select(Num))
                + "],\"best_family\":" + Str(famName[bestFam]) + ",\"rl_family_mix_pct\":[" + string.Join(",", mix.Select(Num)) + "]}");
        }
        Json.Add("\"combat\":{\"runs\":" + NC + ",\"families\":[\"Geese\",\"Stealth geese\",\"Skunks\",\"Deer\"],\"rows\":[" + string.Join(",", rowsJson) + "]}");
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
