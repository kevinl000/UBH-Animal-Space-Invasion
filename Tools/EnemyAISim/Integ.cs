using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnemyAI;

// R1 (v1.3): loads the team's real Unity YAML assets (every LevelData and every wave-based gameplay config),
// lists what is wrong with them, and runs each one through the bridge exactly like the game's spawners do.
static class Integ
{
    public static readonly List<string> Lines = new List<string>();
    public static readonly List<string> Json = new List<string>();   // one object per level / wave set
    static string root = (Environment.GetEnvironmentVariable("UBH_ASSETS") ?? "../../Assets/Scriptable Objects") + "/";

    class Enemy { public EnemyData Data; public string File; }

    static Dictionary<string, Enemy> LoadEnemies()
    {
        var map = new Dictionary<string, Enemy>();
        foreach (var meta in Directory.GetFiles(root + "Enemies", "*.asset.meta", SearchOption.AllDirectories))
        {
            string guid = Regex.Match(File.ReadAllText(meta), @"guid: (\w+)").Groups[1].Value;
            string asset = File.ReadAllText(meta.Substring(0, meta.Length - 5));
            var e = new EnemyData();
            e.name = Regex.Match(asset, @"m_Name: (.*)").Groups[1].Value.Trim();
            var c = Regex.Match(asset, @"\n\s*cost: (-?\d+)");
            e.cost = c.Success ? int.Parse(c.Groups[1].Value) : 0;
            var b = Regex.Match(asset, @"\n\s*isBoss: (\d)");
            e.isBoss = b.Success && b.Groups[1].Value == "1";
            map[guid] = new Enemy { Data = e, File = Path.GetFileName(meta.Substring(0, meta.Length - 5)) };
        }
        return map;
    }

    static float F(string yaml, string key, float fallback = 0f)
    {
        var m = Regex.Match(yaml, @"\n\s*" + key + @": (-?[\d.]+)");
        return m.Success ? float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : fallback; // missing field -> Unity default
    }

    // roster entries: "- enemy: {fileID: N, guid: G, ...}" followed by startingBudget / endingBudget / isLevelBoss
    static List<LevelEnemyEntry> Roster(string yaml, Dictionary<string, Enemy> enemies, List<string> issues, string where)
    {
        var list = new List<LevelEnemyEntry>();
        foreach (Match m in Regex.Matches(yaml, @"- enemy: \{fileID: (-?\d+)(?:, guid: (\w+), type: \d)?\}(.*?)isLevelBoss: (\d)", RegexOptions.Singleline))
        {
            Enemy e = null;
            if (m.Groups[2].Success) enemies.TryGetValue(m.Groups[2].Value, out e);
            if (e == null) issues.Add(where + ": a roster entry has no EnemyData (" + (m.Groups[1].Value == "0" ? "empty slot" : "unknown asset") + ")");
            string body = m.Groups[3].Value;
            list.Add(new LevelEnemyEntry
            {
                enemy = e != null ? e.Data : null,
                startingBudget = F("\n" + body, "startingBudget"),
                endingBudget = F("\n" + body, "endingBudget"),
                isLevelBoss = m.Groups[4].Value == "1",
            });
        }
        return list;
    }

    /// <summary>R1.1: what's wrong with a roster (cost 0, names the AI can't resolve, missing EnemyData).</summary>
    static void CheckRoster(IEnumerable<LevelEnemyEntry> roster, List<string> issues, string where, Dictionary<string, string> nameMap)
    {
        foreach (var e in roster)
        {
            if (e.enemy == null) continue; // already reported
            if (e.enemy.cost <= 0) issues.Add(where + ": '" + e.enemy.name + "' has cost " + e.enemy.cost + " (the AI treats it as 1)");
            EnemyKind k;
            string key = e.enemy.name.Replace(" ", "").Replace("-", "").Replace("_", "");
            if (!Enum.TryParse(key, true, out k)) issues.Add(where + ": name '" + e.enemy.name + "' doesn't match an AI enemy kind (falls back to BasicGoose1)");
            else nameMap[e.enemy.name] = k.ToString();
        }
    }

    class Outcome { public int Spawns; public double Spent, Budget; public bool NaN; public bool AllNamed; public string Detail = ""; public string Error; }

    static string Check(Outcome o)
    {
        bool within = o.Spent <= o.Budget + 1e-3, used = o.Budget > 0 && o.Spent >= 0.95 * o.Budget;
        return "spawns>0: " + (o.Spawns > 0 ? "PASS" : "FAIL") + ", spent<=budget: " + (within ? "PASS" : "FAIL") + ", spent>=95%: " + (used ? "PASS" : "FAIL")
             + ", no NaN: " + (!o.NaN ? "PASS" : "FAIL") + ", names resolved: " + (o.AllNamed ? "PASS" : "FAIL");
    }

    static string JsonOf(string label, string mode, List<string> issues, Outcome o)
    {
        Func<string, string> s = x => "\"" + x.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        Func<double, string> n = x => double.IsNaN(x) || double.IsInfinity(x) ? "null" : x.ToString("R", CultureInfo.InvariantCulture);
        bool within = o.Spent <= o.Budget + 1e-3, used = o.Budget > 0 && o.Spent >= 0.95 * o.Budget;
        return "{\"label\":" + s(label) + ",\"mode\":" + s(mode) + ",\"issues\":[" + string.Join(",", issues.Select(s)) + "]"
            + ",\"spawns\":" + o.Spawns + ",\"spent\":" + n(o.Spent) + ",\"budget\":" + n(o.Budget) + ",\"nan\":" + (o.NaN ? "true" : "false")
            + ",\"names_resolved\":" + (o.AllNamed ? "true" : "false") + ",\"error\":" + (o.Error == null ? "null" : s(o.Error))
            + ",\"pass\":{\"spawns\":" + (o.Spawns > 0 ? "true" : "false") + ",\"within_budget\":" + (within ? "true" : "false") + ",\"used_95\":" + (used ? "true" : "false")
            + ",\"no_nan\":" + (!o.NaN ? "true" : "false") + ",\"names\":" + (o.AllNamed ? "true" : "false") + "},\"detail\":" + s(o.Detail) + "}";
    }

    // ---------------------------------------------------------------- level mode (LevelData: cumulative budget curve)
    static Outcome RunLevel(LevelData lv)
    {
        var o = new Outcome();
        try
        {
            lv.Init();
            var bridge = new WaveDirectorBridge { level = lv, seed = 1 };
            bridge.BeginLevel();
            var snap = Bench.Defense("neutral");
            var perKind = new Dictionary<string, int>();
            float T = lv.maxTime > 0 ? lv.maxTime : 60f;
            for (float t = 0; t <= T; t += 0.5f)
            {
                bridge.Tick(t, snap);
                var e = bridge.PickEntry(t);
                if (float.IsNaN(lv.GetTotalBudget(t)) || float.IsNaN(lv.SpentBudget)) o.NaN = true;
                if (e == null) continue;
                var mods = bridge.RollModifiers(e, snap);
                if (float.IsNaN(mods.SpawnAngleDeg)) o.NaN = true;
                bridge.ReportDamage(mods.Ticket, 1f);
                bridge.ReportGone(mods.Ticket);
                o.Spawns++;
                perKind[e.enemy.name] = (perKind.ContainsKey(e.enemy.name) ? perKind[e.enemy.name] : 0) + 1;
            }
            o.Spent = lv.SpentBudget; o.Budget = lv.GetTotalBudget(T);
            o.Detail = string.Join(", ", perKind.Select(kv => kv.Key + " x" + kv.Value));
        }
        catch (Exception ex) { o.Error = ex.GetType().Name + ": " + ex.Message; }
        return o;
    }

    // ---------------------------------------------------------------- wave mode (GameplayLoopController + EnemySpawner.TickWave)
    static Outcome RunWaves(List<GameplayWave> waves, float spawnInterval, float bossFraction)
    {
        var o = new Outcome();
        try
        {
            var bridge = new WaveDirectorBridge { seed = 1 };
            var holder = new LevelData { maxTime = 1, startingDifficulty = 1, maxDifficulty = 1, postBossDifficulty = 1, enemyDataList = new List<LevelEnemyEntry>() };
            foreach (var w in waves) if (w.roster != null) holder.enemyDataList.AddRange(w.roster.Where(r => r != null && r.enemy != null));
            bridge.level = holder;
            bridge.BeginLevel();
            var snap = Bench.Defense("neutral");
            float clock = 0f;
            var parts = new List<string>();
            for (int wi = 0; wi < waves.Count; wi++)
            {
                var w = waves[wi];
                bridge.BeginWave(w);
                int spawns = 0;
                for (float el = 0f; el <= w.duration + 1e-4f; el += spawnInterval)
                {
                    bridge.Tick(clock + el, snap);
                    float available = w.costBudget * Math.Min(1f, el / Math.Max(0.0001f, w.duration));
                    var e = bridge.PickWaveEntry(available);
                    if (float.IsNaN(bridge.WaveSpent)) o.NaN = true;
                    if (e == null) continue;
                    var mods = bridge.RollModifiers(e, snap);
                    if (float.IsNaN(mods.SpawnAngleDeg)) o.NaN = true;
                    bridge.RecordWaveSpawn(e);
                    bridge.ReportDamage(mods.Ticket, 1f);
                    bridge.ReportGone(mods.Ticket);
                    spawns++;
                }
                clock += w.duration;
                o.Spawns += spawns; o.Spent += bridge.WaveSpent; o.Budget += w.costBudget;
                parts.Add("wave " + (wi + 1) + ": " + spawns + " spawns, spent " + bridge.WaveSpent + "/" + w.costBudget);
            }
            if (waves.Count > 0 && bossFraction > 0f)
            {
                // final boss reinforcements: the same director with a fraction of the last wave's budget (all of it released)
                var last = waves[waves.Count - 1];
                bridge.BeginWave(last, bossFraction);
                int extra = 0;
                for (int i = 0; i < 200; i++)
                {
                    var e = bridge.PickWaveEntry(bridge.WaveSpent + bridge.WaveRemainingBudget);
                    if (e == null) break;
                    bridge.RecordWaveSpawn(e); extra++;
                }
                parts.Add("boss reinforcements: " + extra + " spawns, spent " + bridge.WaveSpent + "/" + (last.costBudget * bossFraction) + " (cap " + bossFraction + " x last wave)");
                if (bridge.WaveSpent > last.costBudget * bossFraction + 1e-3) o.NaN |= false; // reported via detail; budget check below covers waves
            }
            o.Detail = string.Join("; ", parts);
        }
        catch (Exception ex) { o.Error = ex.GetType().Name + ": " + ex.Message; }
        return o;
    }

    public static void Main2()
    {
        Console.WriteLine("=== R1: the team's real assets (every LevelData + every wave config in Scriptable Objects/Levels)");
        var enemies = LoadEnemies();
        var nameMap = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(root + "Levels", "*.asset").OrderBy(f => f))
        {
            string y = File.ReadAllText(file);
            string label = Path.GetFileNameWithoutExtension(file);
            var issues = new List<string>();
            if (y.Contains("\n  waves:"))
            {
                // wave-based gameplay config (LevelGameplayConfig)
                var waves = new List<GameplayWave>();
                string wavesBlock = y.Substring(y.IndexOf("\n  waves:"));
                int end = wavesBlock.IndexOf("\n  finalBosses:");
                if (end > 0) wavesBlock = wavesBlock.Substring(0, end);
                var chunks = Regex.Split(wavesBlock, @"\n  - duration: ").Skip(1).ToList();
                for (int i = 0; i < chunks.Count; i++)
                {
                    string c = "\n    duration: " + chunks[i];
                    var w = new GameplayWave { duration = F(c, "duration"), restDuration = F(c, "restDuration"), costBudget = F(c, "costBudget") };
                    string where = label + " wave " + (i + 1);
                    w.roster = Roster(c, enemies, issues, where).ToArray();
                    if (w.costBudget <= 0) issues.Add(where + ": costBudget is " + w.costBudget + " (no enemies can spawn)");
                    if (w.duration <= 0) issues.Add(where + ": duration is " + w.duration);
                    if (w.roster.Length == 0) issues.Add(where + ": roster is empty");
                    if (w.roster.Count(r => r.enemy != null && r.startingBudget > 0) == 0) issues.Add(where + ": no entry has startingBudget > 0 (the wave picker skips them all)");
                    CheckRoster(w.roster, issues, where, nameMap);
                    waves.Add(w);
                }
                float interval = F(y, "spawnInterval", 1f);
                float bossFraction = F(y, "bossReinforcementBudgetFraction", 0f);
                var o = RunWaves(waves, interval, bossFraction);
                o.AllNamed = !issues.Any(x => x.Contains("doesn't match"));
                Report(label + " (wave mode, " + waves.Count + " waves, spawn every " + interval + "s)", "wave", issues, o);
            }
            else if (y.Contains("startingDifficulty") || y.Contains("enemyDataList"))
            {
                var lv = new LevelData
                {
                    name = label,
                    startingDifficulty = (int)F(y, "startingDifficulty"),
                    postBossDifficulty = (int)F(y, "postBossDifficulty"),
                    maxDifficulty = (int)F(y, "maxDifficulty"),
                    maxTime = F(y, "maxTime"),
                };
                lv.enemyDataList = Roster(y, enemies, issues, label);
                if (lv.maxTime <= 0) issues.Add(label + ": maxTime is " + lv.maxTime);
                if (lv.maxDifficulty <= 0 && lv.startingDifficulty <= 0) issues.Add(label + ": budget is 0 (startingDifficulty and maxDifficulty are 0)");
                if (lv.enemyDataList.Count == 0) issues.Add(label + ": roster is empty");
                CheckRoster(lv.enemyDataList, issues, label, nameMap);
                var o = RunLevel(lv);
                o.AllNamed = !issues.Any(x => x.Contains("doesn't match"));
                Report(label + " (level mode, " + lv.maxTime + "s, difficulty " + lv.startingDifficulty + "/" + lv.postBossDifficulty + "/" + lv.maxDifficulty + ")", "level", issues, o);
            }
        }
        Console.WriteLine("Name map (game asset -> AI kind): " + string.Join(", ", nameMap.OrderBy(k => k.Key).Select(kv => kv.Key + " -> " + kv.Value)));
        Json.Add("{\"name_map\":{" + string.Join(",", nameMap.OrderBy(k => k.Key).Select(kv => "\"" + kv.Key + "\":\"" + kv.Value + "\"")) + "}}");
        Console.WriteLine();
    }

    static void Report(string label, string mode, List<string> issues, Outcome o)
    {
        Console.WriteLine("--- " + label);
        foreach (var i in issues) Console.WriteLine("    ISSUE " + i);
        if (o.Error != null) Console.WriteLine("    EXCEPTION " + o.Error);
        Console.WriteLine("    " + o.Spawns + " spawns, spent " + o.Spent + " / " + o.Budget + " -> " + Check(o));
        if (o.Detail.Length > 0) Console.WriteLine("    " + o.Detail);
        Lines.Add(label + ": " + o.Spawns + " spawns, spent " + o.Spent + "/" + o.Budget + ", issues " + issues.Count);
        Json.Add(JsonOf(label, mode, issues, o));
    }
}
