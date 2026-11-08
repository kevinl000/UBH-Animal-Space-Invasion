using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnemyAI;

/// <summary>Cap experiment (R4d) hook. Only the patched learner build (run.sh r4d) reads it; the game's learner is untouched.</summary>
public static class SimCap { public static double Gamma = 1.0; }

/// <summary>
/// v1.3 experiments. Every condition uses the same seeds 0..N-1 (common random numbers) and writes the raw
/// per-seed values to results13*.json, so report13.py can judge every comparison by paired differences.
/// </summary>
static class V13
{
    public const int N = 200;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static string Num(double x) { return double.IsNaN(x) || double.IsInfinity(x) ? "null" : x.ToString("R", Inv); }
    static string Arr(IEnumerable<double> xs) { return "[" + string.Join(",", xs.Select(Num)) + "]"; }
    static string Str(string x) { return "\"" + x.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }
    static readonly Variant[] CombatVariants = { Variant.Random, Variant.DesignerOnly, Variant.RulesOnly, Variant.LearningOnly, Variant.RulesPlusLearning };
    static IEnumerable<int> Seeds { get { return Enumerable.Range(0, N); } }

    public static void Main13(string which)
    {
        var parts = new List<string> { "\"n\":" + N, "\"seeds\":" + Arr(Seeds.Select(s => (double)s)), "\"generated_utc\":" + Str(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")) };
        if (which == "v13" || which == "abstract") parts.Add("\"abstract\":" + Abstract());
        if (which == "v13" || which == "combat") parts.Add("\"combat\":{\"fuel_old\":" + CombatSet(false) + ",\"fuel_new\":" + CombatSet(true) + "}");
        if (which == "v13" || which == "r4a") parts.Add("\"r4a\":" + R4a());
        if (which == "v13" || which == "r4b") parts.Add("\"r4b\":" + R4b());
        if (which == "v13" || which == "r1") { Integ.Main2(); parts.Add("\"r1\":[" + string.Join(",", Integ.Json) + "]"); }
        string file = which == "v13" ? "results13.json" : "results13_" + which + ".json";
        System.IO.File.WriteAllText(file, "{" + string.Join(",", parts) + "}");
        Console.WriteLine("Wrote " + file);
    }

    // ---------------------------------------------------------------- Abstract model (Test 1, 5, 6 with raw per-seed values)
    static string Abstract()
    {
        Console.WriteLine("=== v1.3 Abstract model, N=" + N);
        Func<float, Fam, double> weakDeer = (t, f) => f == Fam.Deer ? 2.0 : 0.2;
        var ab = new List<string>();
        double oracle = Seeds.Select(s => Bench.Run(Variant.Oracle, s, weakDeer, "aoe").Efficiency).Average();
        foreach (Variant v in Enum.GetValues(typeof(Variant)))
        {
            var rs = Seeds.Select(s => Bench.Run(v, s, weakDeer, "aoe")).ToList();
            ab.Add(Str(v.ToString()) + ":{\"deer_share\":" + Arr(rs.Select(r => r.FamShare[(int)Fam.Deer])) + ",\"pct_of_oracle\":" + Arr(rs.Select(r => 100 * r.Efficiency / oracle))
                + ",\"fam_share\":[" + string.Join(",", rs.Select(r => Arr(r.FamShare))) + "],\"budget_violations\":" + rs.Count(r => r.BudgetViolated) + "}");
        }
        var wr = new List<string>();
        foreach (var v in new[] { Variant.RulesOnly, Variant.RulesPlusLearning })
        {
            var rs = Seeds.Select(s => Bench.Run(v, s, weakDeer, "noradar")).ToList();
            wr.Add(Str(v.ToString()) + ":{\"deer_share\":" + Arr(rs.Select(r => r.FamShare[(int)Fam.Deer])) + ",\"stealth_share\":" + Arr(rs.Select(r => r.FamShare[(int)Fam.Stealth]))
                + ",\"efficiency\":" + Arr(rs.Select(r => r.Efficiency)) + "}");
        }
        Func<float, Fam, double> flat = (t, f) => 0.7;
        var nw = new List<string>();
        foreach (var v in new[] { Variant.DesignerOnly, Variant.RulesOnly, Variant.RulesPlusLearning })
        {
            var rs = Seeds.Select(s => Bench.Run(v, s, flat, "neutral")).ToList();
            nw.Add(Str(v.ToString()) + ":{\"entropy\":" + Arr(rs.Select(r => Entropy(r.FamShare))) + ",\"largest\":" + Arr(rs.Select(r => r.FamShare.Max())) + "}");
        }
        return "{\"ablation\":{" + string.Join(",", ab) + "},\"oracle_efficiency\":" + Num(oracle) + ",\"wrong_rules\":{" + string.Join(",", wr) + "},\"no_weakness\":{" + string.Join(",", nw) + "}}";
    }

    // ---------------------------------------------------------------- Abstract: strategy switch (skunk -> deer at 90s), per seed, rules only vs rules + learning
    public static void MainSwitch()
    {
        Console.WriteLine("=== v1.3 Abstract switch, N=" + N);
        var outp = new List<string>();
        foreach (var v in new[] { Variant.RulesOnly, Variant.RulesPlusLearning })
        {
            var w = new List<double>[3] { new List<double>(), new List<double>(), new List<double>() };
            foreach (int s in Seeds)
            {
                double[] dc = new double[3], tc = new double[3];
                Func<float, Fam, double> m = (t, f) => t < 90 ? (f == Fam.Skunk ? 2.0 : 0.2) : (f == Fam.Deer ? 2.0 : 0.2);
                Bench.Run(v, s, m, "neutral", 360, 800, (t, b, lv, k) =>
                {
                    if (k == null || t < 90 || t >= 180) return;
                    int i = (int)((t - 90) / 30);
                    double c = EnemyCatalog.DefaultWeight[k.Value];
                    tc[i] += c; if (EnemyCatalog.IsDeer(k.Value)) dc[i] += c;
                }, v == Variant.RulesPlusLearning ? new EnemyLearnerSettings { DecayPerStep = 0.85f } : null);
                for (int i = 0; i < 3; i++) w[i].Add(dc[i] / Math.Max(1, tc[i]));
            }
            outp.Add(Str(v.ToString()) + ":{\"deer_share_after\":[" + Arr(w[0]) + "," + Arr(w[1]) + "," + Arr(w[2]) + "]}");
        }
        System.IO.File.WriteAllText("results13_switch.json", "{\"n\":" + N + "," + string.Join(",", outp) + "}");
        Console.WriteLine("Wrote results13_switch.json");
    }

    // ---------------------------------------------------------------- Combat model: 5 loadouts x 5 variants + hindsight family-only, per fuel model
    static string CombatSet(bool newFuel)
    {
        Console.WriteLine("=== v1.3 Combat model, fuel " + (newFuel ? "NEW (per shot)" : "OLD (continuous)") + ", N=" + N);
        var rows = new List<string>();
        foreach (var lo in Combat.Loadouts.Keys)
        {
            var vs = new List<string>();
            foreach (var v in CombatVariants)
            {
                bool post = v == Variant.LearningOnly || v == Variant.RulesPlusLearning;
                var rs = Seeds.Select(s => Combat.Run(v, s, lo, new CombatOptions { NewFuel = newFuel, Posterior = post })).ToList();
                string extra = post ? ",\"posterior_mean\":[" + string.Join(",", rs.Select(r => Arr(r.PosteriorMean))) + "],\"factor_at_cap\":[" + string.Join(",", rs.Select(r => Arr(r.FactorAtCap)))
                    + "],\"draw_mean\":[" + string.Join(",", rs.Select(r => Arr(r.DrawMean))) + "]" : "";
                vs.Add(Str(v.ToString()) + ":{\"pressure\":" + Arr(rs.Select(r => r.Pressure)) + ",\"planet\":" + Arr(rs.Select(r => r.PlanetDamage))
                    + ",\"towers_lost\":" + Arr(rs.Select(r => (double)r.TowersLost)) + ",\"mix\":[" + string.Join(",", rs.Select(r => Arr(r.FamCost.Select(c => r.Spent > 0 ? c / r.Spent : 0)))) + "]" + extra + "}");
            }
            var fam = new List<string>();
            for (int f = 0; f < 4; f++)
            {
                int ff = f;
                fam.Add(Arr(Seeds.Select(s => Combat.Run(Variant.DesignerOnly, s, lo, new CombatOptions { Only = (Fam)ff, NewFuel = newFuel }).Pressure)));
            }
            rows.Add(Str(lo) + ":{\"variants\":{" + string.Join(",", vs) + "},\"family_only\":[" + string.Join(",", fam) + "]}");
            Console.WriteLine("  " + lo + " done");
        }
        return "{" + string.Join(",", rows) + "}";
    }

    // ---------------------------------------------------------------- R4a: stealth share fixed at 0..100%
    public static readonly double[] Shares = { 0, 0.10, 0.25, 0.50, 0.75, 1.0 };
    static string R4a()
    {
        Console.WriteLine("=== R4a: stealth share fixed, designer mix otherwise, N=" + N);
        var rows = new List<string>();
        foreach (var lo in Combat.Loadouts.Keys)
        {
            var cells = new List<string>();
            foreach (double p in Shares)
            {
                var rs = Seeds.Select(s => Combat.Run(Variant.DesignerOnly, s, lo, new CombatOptions { StealthShare = p })).ToList();
                cells.Add("{\"share\":" + Num(p) + ",\"pressure\":" + Arr(rs.Select(r => r.Pressure)) + ",\"realized\":" + Arr(rs.Select(r => r.Spent > 0 ? r.FamCost[(int)Fam.Stealth] / r.Spent : 0)) + "}");
            }
            rows.Add(Str(lo) + ":[" + string.Join(",", cells) + "]");
        }
        return "{" + string.Join(",", rows) + "}";
    }

    // ---------------------------------------------------------------- R4b: +-50% on the values the README doesn't give
    static string R4b()
    {
        Console.WriteLine("=== R4b: sensitivity of the assumed values (+-50%), N=" + N);
        var settings = new List<Tuple<string, CombatOptions>>
        {
            Tuple.Create("fire rate x0.5", new CombatOptions { FireRateMul = 0.5f }),
            Tuple.Create("fire rate x1.5", new CombatOptions { FireRateMul = 1.5f }),
            Tuple.Create("range x0.5", new CombatOptions { RangeMul = 0.5f }),
            Tuple.Create("range x1.5", new CombatOptions { RangeMul = 1.5f }),
            Tuple.Create("radar reveal x0.5", new CombatOptions { RevealMul = 0.5f }),
            Tuple.Create("radar reveal x1.5", new CombatOptions { RevealMul = 1.5f }),
        };
        var rows = new List<string>();
        foreach (var st in settings)
        {
            var los = new List<string>();
            foreach (var lo in Combat.Loadouts.Keys)
            {
                var fam = new List<string>();
                for (int f = 0; f < 4; f++)
                {
                    var o = st.Item2; int ff = f;
                    fam.Add(Arr(Seeds.Select(s => Combat.Run(Variant.DesignerOnly, s, lo, new CombatOptions { Only = (Fam)ff, FireRateMul = o.FireRateMul, RangeMul = o.RangeMul, RevealMul = o.RevealMul }).Pressure)));
                }
                var rl = Seeds.Select(s => Combat.Run(Variant.RulesPlusLearning, s, lo, new CombatOptions { FireRateMul = st.Item2.FireRateMul, RangeMul = st.Item2.RangeMul, RevealMul = st.Item2.RevealMul })).ToList();
                los.Add(Str(lo) + ":{\"family_only\":[" + string.Join(",", fam) + "],\"rl_pressure\":" + Arr(rl.Select(r => r.Pressure))
                    + ",\"rl_mix\":[" + string.Join(",", rl.Select(r => Arr(r.FamCost.Select(c => r.Spent > 0 ? c / r.Spent : 0)))) + "]}");
            }
            rows.Add(Str(st.Item1) + ":{" + string.Join(",", los) + "}");
            Console.WriteLine("  " + st.Item1 + " done");
        }
        return "{" + string.Join(",", rows) + "}";
    }

    // ---------------------------------------------------------------- R4d: learned-factor cap 2x / 4x / none (patched learner build only)
    public static void MainCap()
    {
        Console.WriteLine("=== R4d: learning cap (patched learner: factor = (theta / prior mean)^gamma), N=" + N);
        var caps = new[] { Tuple.Create("2x (current)", 1.0, 2f, 0.33f), Tuple.Create("4x", 2.0, 4f, 0.33f * 0.33f), Tuple.Create("no cap", 8.0, 1e9f, 0f) };
        var rows = new List<string>();
        foreach (var c in caps)
        {
            SimCap.Gamma = c.Item2;
            var los = new List<string>();
            foreach (var lo in Combat.Loadouts.Keys)
            {
                var cc = c;
                var rs = Seeds.Select(s => Combat.Run(Variant.RulesPlusLearning, s, lo, new CombatOptions
                {
                    Configure = b => { b.Director.Learner.Settings.MaxFactor = cc.Item3; b.Director.Learner.Settings.MinFactor = cc.Item4; }
                })).ToList();
                los.Add(Str(lo) + ":{\"pressure\":" + Arr(rs.Select(r => r.Pressure)) + ",\"entropy\":" + Arr(rs.Select(r => Entropy(r.FamCost.Select(x => r.Spent > 0 ? x / r.Spent : 0).ToArray())))
                    + ",\"mix\":[" + string.Join(",", rs.Select(r => Arr(r.FamCost.Select(x => r.Spent > 0 ? x / r.Spent : 0)))) + "]}");
            }
            rows.Add("{\"cap\":" + Str(c.Item1) + ",\"gamma\":" + Num(c.Item2) + ",\"loadouts\":{" + string.Join(",", los) + "}}");
            Console.WriteLine("  cap " + c.Item1 + " done");
        }
        SimCap.Gamma = 1.0;
        System.IO.File.WriteAllText("results13_r4d.json", "{\"n\":" + N + ",\"caps\":[" + string.Join(",", rows) + "]}");
        Console.WriteLine("Wrote results13_r4d.json");
    }

    public static double Entropy(double[] p)
    {
        double h = 0;
        foreach (double x in p) if (x > 0) h -= x * Math.Log(x);
        return h / Math.Log(p.Length);
    }
}
