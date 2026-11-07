using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnemyAI;

// Loads the team's real Unity YAML assets (LevelData + EnemyData) and runs a spawner loop through the bridge.
static class Integ
{
    static string root = (Environment.GetEnvironmentVariable("UBH_ASSETS") ?? "../../Assets/Scriptable Objects") + "/";

    static Dictionary<string, EnemyData> LoadEnemies()
    {
        var map = new Dictionary<string, EnemyData>();
        foreach (var meta in Directory.GetFiles(root + "Enemies", "*.asset.meta"))
        {
            string guid = Regex.Match(File.ReadAllText(meta), @"guid: (\w+)").Groups[1].Value;
            string asset = File.ReadAllText(meta.Substring(0, meta.Length - 5));
            var e = new EnemyData();
            e.name = Regex.Match(asset, @"m_Name: (.*)").Groups[1].Value.Trim();
            var c = Regex.Match(asset, @"\n\s*cost: (-?\d+)");
            e.cost = c.Success ? int.Parse(c.Groups[1].Value) : 0;
            var b = Regex.Match(asset, @"\n\s*isBoss: (\d)");
            e.isBoss = b.Success && b.Groups[1].Value == "1";
            map[guid] = e;
        }
        return map;
    }

    static float F(string yaml, string key)
    {
        var m = Regex.Match(yaml, @"\n  " + key + @": (-?[\d.]+)");
        return m.Success ? float.Parse(m.Groups[1].Value) : 0f; // missing field -> Unity default 0
    }

    static LevelData LoadLevel(string file, Dictionary<string, EnemyData> enemies)
    {
        string y = File.ReadAllText(root + file);
        var lv = new LevelData
        {
            startingDifficulty = (int)F(y, "startingDifficulty"),
            postBossDifficulty = (int)F(y, "postBossDifficulty"),
            maxDifficulty = (int)F(y, "maxDifficulty"),
            maxTime = F(y, "maxTime"),
            enemyDataList = new List<LevelEnemyEntry>()
        };
        foreach (Match m in Regex.Matches(y, @"- enemy: \{fileID: \d+, guid: (\w+), type: \d\}\s*\n\s*startingBudget: (-?[\d.]+)\s*\n\s*endingBudget: (-?[\d.]+)\s*\n\s*isLevelBoss: (\d)"))
        {
            EnemyData e; enemies.TryGetValue(m.Groups[1].Value, out e);
            lv.enemyDataList.Add(new LevelEnemyEntry { enemy = e, startingBudget = float.Parse(m.Groups[2].Value), endingBudget = float.Parse(m.Groups[3].Value), isLevelBoss = m.Groups[4].Value == "1" });
        }
        return lv;
    }

    static void RunLoop(string label, LevelData lv, float seconds)
    {
        Console.WriteLine("--- " + label);
        Console.WriteLine("    maxTime=" + lv.maxTime + " difficulty " + lv.startingDifficulty + "/" + lv.postBossDifficulty + "/" + lv.maxDifficulty
            + ", roster: " + string.Join(", ", lv.enemyDataList.Select(e => (e.enemy == null ? "<missing>" : e.enemy.name + "(cost " + e.enemy.cost + ")") + " share " + e.startingBudget + "->" + e.endingBudget)));
        try
        {
            lv.Init();
            var bridge = new WaveDirectorBridge { level = lv, seed = 1 };
            bridge.BeginLevel();
            var snap = Bench.Defense("neutral");
            int spawns = 0; bool nan = false;
            var perKind = new Dictionary<string, int>();
            for (float t = 0; t <= seconds; t += 0.5f)
            {
                bridge.Tick(t, snap);
                var e = bridge.PickEntry(t);
                if (float.IsNaN(lv.GetTotalBudget(t)) || float.IsNaN(lv.SpentBudget)) nan = true;
                if (e == null) continue;
                var mods = bridge.RollModifiers(e, snap);
                if (float.IsNaN(mods.SpawnAngleDeg)) nan = true;
                bridge.ReportDamage(mods.Ticket, 1f);
                bridge.ReportGone(mods.Ticket);
                spawns++;
                string k = e.enemy.name; perKind[k] = (perKind.ContainsKey(k) ? perKind[k] : 0) + 1;
            }
            Console.WriteLine("    result: " + spawns + " spawns in " + seconds + "s (" + string.Join(", ", perKind.Select(kv => kv.Key + " x" + kv.Value)) + "), NaN seen: " + nan
                + ", spent " + lv.SpentBudget + " / budget at end " + lv.GetTotalBudget(seconds) + ", intent: '" + bridge.Intent + "'");
        }
        catch (Exception ex)
        {
            Console.WriteLine("    EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    public static void Main2()
    {
        Console.WriteLine("=== Test 3: integration with the team's real assets");
        var enemies = LoadEnemies();
        var lv = LoadLevel("Level1.asset", enemies);
        RunLoop("Level1.asset exactly as it is in the repo", lv, 60);

        // Same assets, but with the values filled in the way the README says (names + costs + level settings)
        var lv2 = LoadLevel("Level1.asset", enemies);
        lv2.maxTime = 120; lv2.startingDifficulty = 20; lv2.postBossDifficulty = 100; lv2.maxDifficulty = 200;
        lv2.enemyDataList[0].startingBudget = 1; lv2.enemyDataList[0].endingBudget = 1;
        RunLoop("Level1 with level settings filled, enemy still cost 0 (placeholder)", lv2, 120);

        var lv3 = LoadLevel("Level1.asset", enemies);
        lv3.maxTime = 120; lv3.startingDifficulty = 20; lv3.postBossDifficulty = 100; lv3.maxDifficulty = 200;
        foreach (var kv in enemies) { kv.Value.cost = kv.Value.name == "EasyEnemy" ? 3 : kv.Value.name == "MediumEnemt" ? 8 : 20; }
        lv3.enemyDataList.Clear();
        foreach (var kv in enemies) lv3.enemyDataList.Add(new LevelEnemyEntry { enemy = kv.Value, startingBudget = 1, endingBudget = 1 });
        RunLoop("All 3 placeholder enemies with costs 3/8/20 (names still don't match)", lv3, 120);
        Console.WriteLine();
    }
}
