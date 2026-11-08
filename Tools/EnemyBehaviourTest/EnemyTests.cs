// Frame-by-frame behaviour tests for the enemy code, run without Unity (see UnityEngineStub.cs).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using EnemyAI;

class FakeBullet : IEnemyHitter
{
    public float Damage { get; set; }
    public bool IgnoresShield { get; set; }
    public bool CanHitStealth { get; set; }
    public int Hits;
    public void OnHitEnemy(BaseEnemyData enemy) { Hits++; }
}

/// <summary>Stands in for the team's BaseTowerData: any GameEntity can use the same PoolManager.</summary>
class FakeTowerEntity : GameEntity
{
    public int SpawnedCalls, DespawnedCalls;
    protected override void OnSpawned() { SpawnedCalls++; }
    protected override void OnDespawned() { DespawnedCalls++; }
}

class FakeTower : IDamageable, IFreezable
{
    public float Hp; public float FrozenFor; public float Taken;
    public FakeTower(float hp) { Hp = hp; }
    public bool IsAlive { get { return Hp > 0; } }
    public float ReceiveDamage(float amount) { float d = Math.Min(amount, Hp); Hp -= d; Taken += d; return d; }
    public void Freeze(float seconds) { FrozenFor = seconds; }
}

static class EnemyTests
{
    static int pass, fail;
    static readonly List<string> Report = new List<string>();

    static void Check(string name, bool ok, string detail)
    {
        if (ok) pass++; else fail++;
        string line = (ok ? "PASS " : "FAIL ") + name + " -> " + detail;
        Report.Add(line);
        Console.WriteLine(line);
    }

    // ------------------------------------------------------------ README data (same numbers as EnemyDataGenerator)
    static readonly Dictionary<string, EnemyData> Data = new Dictionary<string, EnemyData>();

    static EnemyData D(string name, float hp, int money, int fuel, float dmg, EnemyAttackType a, EnemyMovement mv, bool shield, bool stealth, bool boss, int cost, float interval, BaseEnemyData prefab)
    {
        var d = new EnemyData { name = name, maxHealth = hp, moneyDrop = money, fuelDrop = fuel, damage = dmg, attackType = a, movement = mv,
            hasShield = shield, isStealth = stealth, isBoss = boss, cost = cost, attackInterval = interval, prefab = prefab };
        Data[name] = d;
        return d;
    }

    static void BuildData()
    {
        Data.Clear();
        var basic = new BasicEnemy(); var deer = new DeerEnemy(); var boss = new BossEnemy();
        D("Basic Goose 1", 1, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot, false, false, false, 3, 1.5f, basic);
        D("Basic Goose 2", 2, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot, false, false, false, 4, 1.5f, basic);
        D("Basic Goose 3", 4, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot, false, false, false, 6, 1.5f, basic);
        D("Shielded Goose", 4, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot, true, false, false, 8, 1.5f, basic);
        D("Stealth Goose", 1, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot, false, true, false, 5, 1.5f, basic);
        D("Basic Skunk 1", 2, 2, 2, 2, EnemyAttackType.AoE, EnemyMovement.StopAndShoot, false, false, false, 6, 2f, basic);
        D("Basic Skunk 2", 4, 2, 2, 2, EnemyAttackType.AoE, EnemyMovement.StopAndShoot, false, false, false, 8, 2f, basic);
        D("Basic Skunk 3", 8, 2, 2, 2, EnemyAttackType.AoE, EnemyMovement.StopAndShoot, false, false, false, 12, 2f, basic);
        D("Shielded Skunk", 8, 2, 2, 2, EnemyAttackType.AoE, EnemyMovement.StopAndShoot, true, false, false, 14, 2f, basic);
        D("Basic Deer", 10, 4, 4, 5, EnemyAttackType.Single, EnemyMovement.OrbitAndCharge, false, false, false, 20, 4f, deer);
        D("Shielded Deer", 10, 4, 4, 5, EnemyAttackType.Single, EnemyMovement.OrbitAndCharge, true, false, false, 22, 4f, deer);

        D("Healthy Goose", 50, 10, 10, 2, EnemyAttackType.Single, EnemyMovement.Orbit, false, false, true, 40, 2f, boss);
        D("Healthy Skunk", 40, 10, 10, 3, EnemyAttackType.AoE, EnemyMovement.Orbit, false, false, true, 40, 3f, boss);
        D("Mother Goose", 40, 15, 15, 0, EnemyAttackType.None, EnemyMovement.Orbit, false, false, true, 40, 8f,
            new MotherGooseBoss { flockEnemy = Data["Basic Goose 3"], flockSize = 4, flockInterval = 8f });
        D("Mother Skunk", 35, 15, 15, 3, EnemyAttackType.AoE, EnemyMovement.Orbit, false, false, true, 40, 4f, new MotherSkunkBoss { bombsPerVolley = 3 });
        D("Restraining Buck", 50, 20, 20, 5, EnemyAttackType.Single, EnemyMovement.Orbit, false, false, true, 40, 4f, new RestrainingBuckBoss { freezeSeconds = 3f });
        D("Sneaky Healthy Goose", 35, 10, 10, 2, EnemyAttackType.Single, EnemyMovement.Orbit, false, false, true, 40, 2f,
            new SneakyHealthyGooseBoss { secondsBetweenStealth = 10f, stealthDuration = 4f, healPerSecond = 2f });
        D("Animal Ship", 25, 5, 5, 0, EnemyAttackType.None, EnemyMovement.Orbit, false, false, true, 40, 1f, new AnimalShipBoss
        {
            cargo = new[] {
                new AnimalShipBoss.Cargo { enemy = Data["Healthy Skunk"], count = 2 },
                new AnimalShipBoss.Cargo { enemy = Data["Healthy Goose"], count = 2 },
                new AnimalShipBoss.Cargo { enemy = Data["Shielded Goose"], count = 5 } }
        });
        D("Endangered Deer", 60, 25, 25, 5, EnemyAttackType.Single, EnemyMovement.Orbit, false, false, true, 40, 4f,
            new EndangeredDeerBoss { guardEnemy = Data["Shielded Deer"], guardInterval = 12f });
    }

    // ------------------------------------------------------------ world setup
    static SimplePlanetDefense planet;
    static EnemySpawner spawner;
    static WaveDirectorBridge bridge;
    static PoolManager pool;

    static void NewWorld(params int[] towerSlots)
    {
        MonoBehaviour.ClearAll();
        BaseEnemyData.Active.Clear();
        UnityEngine.Random.Rng = new System.Random(7);
        UnityEngine.Debug.Logs.Clear();
        pool = new PoolManager();
        Register(pool);
        planet = new SimplePlanetDefense { radius = 100f, slots = 12, rotationDegreesPerSecond = 0f, slot0Angle = 90f, planetMaxHealth = 100f };
        Register(planet);
        foreach (int s in towerSlots) planet.RegisterTower(s, TowerKind.Laser, new FakeTower(25));
        bridge = new WaveDirectorBridge { seed = 1 };
        Register(bridge);
        spawner = new EnemySpawner { planetDefense = planet, bridge = bridge, startOnAwake = false, spawnDistance = 4f };
        Register(spawner);
    }

    static void Register(MonoBehaviour mb) { MonoBehaviour.Register(mb); }

    static FakeTower Tower(int slot)
    {
        var f = typeof(SimplePlanetDefense).GetField("slotTowers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var arr = (Array)f.GetValue(planet);
        var s = arr.GetValue(slot);
        return s == null ? null : (FakeTower)s.GetType().GetField("Tower").GetValue(s);
    }

    static void Run(float seconds, float dt = 0.05f)
    {
        for (float t = 0; t < seconds; t += dt) MonoBehaviour.Frame(dt);
    }

    static BaseEnemyData Spawn(string name, float angle = 90f, bool shiny = false, bool orbit = false, int ticket = 0)
    {
        return spawner.Spawn(Data[name], angle, shiny, orbit, ticket, 4f);
    }

    static float SlotAngle(int slot) { return planet.SlotAngle(slot); }

    // ------------------------------------------------------------ tests
    static void Main(string[] args)
    {
        string readme = args.Length > 0 ? args[0] : null;
        Console.WriteLine("=== Enemy behaviour tests");
        BuildData();

        ReadmeMatchesGenerator(readme);

        // Goose: fly in, stop, hit the tower below every 1.5s for 1 damage
        NewWorld(0);
        var g = Spawn("Basic Goose 2", SlotAngle(0));
        Run(5f); // 4 -> 1.8 radii at 0.5 radii/s takes 4.4s
        bool stopped = Math.Abs(g.Distance - Data["Basic Goose 2"].stopDistance) < 1e-3;
        Run(13f);
        var t0 = Tower(0);
        Check("Goose stops at stopDistance and shoots the tower below", stopped && t0.Taken >= 9 && t0.Taken <= 13 && planet.PlanetHealth == 100f,
            "distance " + g.Distance.ToString("F2") + ", tower took " + t0.Taken + " (README dmg 1 every 1.5s over ~15s), planet HP " + planet.PlanetHealth);

        // Goose over an empty slot hits the planet
        NewWorld();
        Spawn("Basic Goose 1", SlotAngle(3));
        Run(10f);
        Check("Goose over an empty slot damages the planet", planet.PlanetHealth < 100f, "planet HP " + planet.PlanetHealth);

        // Skunk AoE: centre + both neighbours
        NewWorld(0, 1, 11);
        Spawn("Basic Skunk 2", SlotAngle(0));
        Run(12f);
        Check("Skunk AoE hits the slot below and both neighbours", Tower(0).Taken > 0 && Tower(1).Taken > 0 && Tower(11).Taken > 0 && Math.Abs(Tower(0).Taken - Tower(1).Taken) < 0.01,
            "taken " + Tower(11).Taken + " / " + Tower(0).Taken + " / " + Tower(1).Taken + " (README dmg 2)");

        // Shield
        NewWorld();
        var sg = Spawn("Shielded Goose");
        sg.TakeDamage(100f);
        bool aliveAfterFirst = sg.IsAlive;
        sg.TakeDamage(4f);
        Check("Shield negates the first hit only", aliveAfterFirst && !sg.IsAlive, "alive after 100 dmg: " + aliveAfterFirst + ", alive after next 4 dmg: " + sg.IsAlive);

        // Stealth
        var st = Spawn("Stealth Goose");
        Check("Stealth goose is only targetable by stealth-revealing towers", !st.IsTargetable(false) && st.IsTargetable(true), "normal tower: " + st.IsTargetable(false) + ", radar: " + st.IsTargetable(true));

        // Shiny: double HP and drops
        NewWorld();
        int money = -1, fuel = -1;
        Action<int, int> onKill = (m, f) => { money = m; fuel = f; };
        EnemyEvents.Killed += onKill;
        var sh = Spawn("Basic Skunk 3", 90f, true);
        float shinyHp = sh.MaxHealth;
        sh.TakeDamage(999f);
        EnemyEvents.Killed -= onKill;
        Check("Shiny doubles HP and drops", shinyHp == 16f && money == 4 && fuel == 4, "HP " + shinyHp + " (README 8), money " + money + ", fuel " + fuel + " (README 2 each)");

        // Pest Trap style DoT ignores shield; slow halves speed
        NewWorld();
        var sd = Spawn("Shielded Deer");
        sd.TakeDamageIgnoringShield(3f);
        Check("Damage over time ignores the shield", sd.HasShield && sd.Health == 7f, "shield kept: " + sd.HasShield + ", HP " + sd.Health);

        // Deer: orbit, charge, hit for 5, return
        NewWorld(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
        var d = Spawn("Basic Deer", SlotAngle(0));
        Run(4f);
        float a0 = d.AngleDegrees;
        float minDist = 99f; float total = 0;
        for (int i = 0; i < 300; i++) { MonoBehaviour.Frame(0.05f); minDist = Math.Min(minDist, d.Distance); }
        for (int s = 0; s < 12; s++) total += Tower(s).Taken;
        Check("Deer orbits, charges to the surface and hits for 5", Math.Abs(d.AngleDegrees - a0) > 1f && minDist <= 1.001f && total > 0 && total % 5 == 0 && Math.Abs(d.Distance - 1.6f) < 0.5f,
            "moved " + PlanetMath.Distance(a0, d.AngleDegrees).ToString("F0") + " deg, closest " + minDist.ToString("F2") + " radii, total tower damage " + total);

        // AI learning gets damage and death reports
        NewWorld(0);
        var lv = new LevelData { maxTime = 60, startingDifficulty = 20, postBossDifficulty = 50, maxDifficulty = 100, enemyDataList = new List<LevelEnemyEntry>() };
        lv.enemyDataList.Add(new LevelEnemyEntry { enemy = Data["Basic Deer"], startingBudget = 1, endingBudget = 1 });
        spawner.level = lv; spawner.Begin();
        var tracked = spawner.Spawn(Data["Basic Deer"], SlotAngle(0), false, true, bridge.Director.Learner.Register(EnemyKind.BasicDeer, 20), 4f);
        Run(12f);
        tracked.TakeDamage(999f);
        // one result with 0 damage would give Beta(1,2) = mean 0.333; any reported damage lifts it above that
        Check("Enemy reports damage and death to the AI's learner", bridge.Director.Learner.SampleCount(EnemyKind.BasicDeer) == 1 && bridge.Director.Learner.MeanReward(EnemyKind.BasicDeer) > 0.34f && (Tower(0).Taken > 0 || planet.PlanetHealth < 100f),
            "deer results learned: " + bridge.Director.Learner.SampleCount(EnemyKind.BasicDeer) + ", mean reward " + bridge.Director.Learner.MeanReward(EnemyKind.BasicDeer).ToString("F2")
            + " (0 damage would be 0.33), damage dealt " + (Tower(0).Taken + 100f - planet.PlanetHealth) + " (deer orbit, so it may charge an empty slot)");

        Bosses();
        FullLevel();
        ProjectileHit();
        PhysicsAndPooling();

        Console.WriteLine("Enemy behaviour: " + pass + " passed, " + fail + " failed");
        if (fail > 0) Environment.ExitCode = 1;
    }

    static int CountByName(string name) { return BaseEnemyData.Active.Count(e => e.name == name && e.IsAlive); }

    static void Bosses()
    {
        // Mother Goose: no attack, flocks of Basic Goose 3
        NewWorld(0);
        int summoned = 0;
        Action<BaseEnemyData> onSpawn = e => { if (e.name == "Basic Goose 3") summoned++; };
        EnemyEvents.Spawned += onSpawn;
        var mg = (MotherGooseBoss)Spawn("Mother Goose", SlotAngle(0), false, true);
        Run(14f);
        EnemyEvents.Spawned -= onSpawn;
        Check("Mother Goose calls flocks of Basic Goose 3 (no attack of its own)", summoned >= mg.flockSize && summoned % mg.flockSize == 0 && Data["Mother Goose"].attackType == EnemyAttackType.None,
            summoned + " Basic Goose 3 summoned in flocks of " + mg.flockSize);

        // Mother Skunk: n bombs per volley
        NewWorld(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
        var ms = (MotherSkunkBoss)Spawn("Mother Skunk", SlotAngle(0), false, true);
        Run(6.5f);
        int hitTowers = Enumerable.Range(0, 12).Count(s => Tower(s).Taken > 0);
        Check("Mother Skunk drops several AoE bombs per volley", hitTowers >= 4, hitTowers + " towers hit (n = " + ms.bombsPerVolley + " bombs, 3 dmg AoE)");

        // Restraining Buck: 5 dmg + freeze the closest tower
        NewWorld(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
        Spawn("Restraining Buck", SlotAngle(0), false, true);
        Run(8f);
        bool frozen = Enumerable.Range(0, 12).Any(s => Tower(s).FrozenFor == 3f);
        float taken = Enumerable.Range(0, 12).Sum(s => Tower(s).Taken);
        Check("Restraining Buck hits for 5 and freezes the closest tower", frozen && taken > 0 && taken % 5 == 0, "frozen: " + frozen + ", damage dealt " + taken);

        // Sneaky Healthy Goose: stealth phase, heals, no attacks while stealthed
        NewWorld(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
        var sn = (SneakyHealthyGooseBoss)Spawn("Sneaky Healthy Goose", SlotAngle(0), false, true);
        Run(4f);
        sn.TakeDamage(20f);
        float hpBefore = sn.Health;
        bool sawStealth = false; float dmgDuringStealth = 0;
        for (int i = 0; i < 400; i++)
        {
            float before = Enumerable.Range(0, 12).Sum(s => Tower(s).Taken);
            MonoBehaviour.Frame(0.05f);
            if (sn.InStealthMode) { sawStealth = true; dmgDuringStealth += Enumerable.Range(0, 12).Sum(s => Tower(s).Taken) - before; }
        }
        Check("Sneaky Healthy Goose goes stealth, heals, and doesn't attack while stealthed", sawStealth && sn.Health > hpBefore && dmgDuringStealth == 0,
            "stealth seen: " + sawStealth + ", HP " + hpBefore + " -> " + sn.Health.ToString("F1") + ", damage during stealth " + dmgDuringStealth);

        // Animal Ship: releases 2 + 2 + 5 on death
        NewWorld();
        var ship = Spawn("Animal Ship", 90f, false, true);
        ship.TakeDamage(999f);
        int hs = CountByName("Healthy Skunk"), hg = CountByName("Healthy Goose"), sgc = CountByName("Shielded Goose");
        Check("Animal Ship releases 2 Healthy Skunk, 2 Healthy Goose, 5 Shielded Goose on death", hs == 2 && hg == 2 && sgc == 5, hs + " / " + hg + " / " + sgc);

        // Endangered Deer: invulnerable while a guard lives
        NewWorld(0);
        var ed = (EndangeredDeerBoss)Spawn("Endangered Deer", SlotAngle(0), false, true);
        Run(6f);
        int guards = ed.LivingGuards;
        ed.TakeDamage(30f);
        float hpGuarded = ed.Health;
        foreach (var gd in BaseEnemyData.Active.Where(e => e.name == "Shielded Deer").ToList()) { gd.TakeDamage(1f); gd.TakeDamage(999f); }
        MonoBehaviour.Frame(0.05f);
        ed.TakeDamage(30f);
        Check("Endangered Deer can't be hurt while a Shielded Deer guard lives", guards >= 1 && hpGuarded == 60f && ed.Health == 30f,
            "guards " + guards + ", HP after hit with guard " + hpGuarded + ", after guards died " + ed.Health);
    }

    static void FullLevel()
    {
        NewWorld(0, 2, 4, 6, 8, 10);
        var lv = new LevelData { maxTime = 120, startingDifficulty = 20, postBossDifficulty = 100, maxDifficulty = 200, enemyDataList = new List<LevelEnemyEntry>() };
        foreach (var name in new[] { "Basic Goose 1", "Basic Goose 2", "Basic Goose 3", "Shielded Goose", "Stealth Goose", "Basic Skunk 1", "Basic Skunk 2", "Basic Skunk 3", "Shielded Skunk", "Basic Deer", "Shielded Deer" })
            lv.enemyDataList.Add(new LevelEnemyEntry { enemy = Data[name], startingBudget = 1, endingBudget = 1 });
        lv.enemyDataList.Add(new LevelEnemyEntry { enemy = Data["Healthy Goose"], isLevelBoss = true, startingBudget = 1, endingBudget = 1 });
        spawner.level = lv;
        int spawned = 0, bosses = 0, warnings = 0, timeUp = 0;
        Action<BaseEnemyData> onSpawn = e => spawned++;
        Action<BaseEnemyData> onBoss = e => bosses++;
        Action<EnemyData> onWarn = e => warnings++;
        Action onTimeUp = () => timeUp++;
        EnemyEvents.Spawned += onSpawn; EnemyEvents.BossSpawned += onBoss; EnemyEvents.BossWarning += onWarn; EnemyEvents.LevelTimeUp += onTimeUp;
        bool overBudget = false; string error = null;
        try
        {
            spawner.Begin();
            for (float t = 0; t < 130; t += 0.05f)
            {
                MonoBehaviour.Frame(0.05f);
                if (lv.SpentBudget > lv.GetTotalBudget(spawner.Elapsed) + 20.001f) overBudget = true; // +max single cost: pending pick may land just as budget allows
                // towers fight back a little so the level doesn't fill up forever
                foreach (var e in BaseEnemyData.Active.ToList()) if (e.IsTargetable(false) && UnityEngine.Random.value < 0.01f) e.TakeDamage(2f);
            }
        }
        catch (Exception ex) { error = ex.GetType().Name + ": " + ex.Message; }
        EnemyEvents.Spawned -= onSpawn; EnemyEvents.BossSpawned -= onBoss; EnemyEvents.BossWarning -= onWarn; EnemyEvents.LevelTimeUp -= onTimeUp;
        var errs = UnityEngine.Debug.Logs.Where(l => l.StartsWith("ERROR")).ToList();
        int instances = MonoBehaviour.LiveCount<BaseEnemyData>();
        Check("Full 120s level through the spawner and the AI (README roster + boss)", error == null && spawned > 10 && bosses == 1 && warnings == 1 && timeUp == 1 && !overBudget && errs.Count == 0,
            (error ?? "no exceptions") + ", " + spawned + " enemies spawned, boss " + bosses + ", boss warning " + warnings + ", time-up " + timeUp
            + ", spent " + lv.SpentBudget + " of " + lv.GetTotalBudget(120) + ", over budget: " + overBudget + ", planet HP " + planet.PlanetHealth.ToString("F0"));
        Check("Full level reuses pooled enemies instead of creating one per spawn", instances < spawned && error == null,
            spawned + " spawns served by " + instances + " instances from the one PoolManager (prewarm 5 per roster prefab), alive at the end " + BaseEnemyData.Active.Count);
    }

    static void ProjectileHit()
    {
        NewWorld(0);
        var withProjectile = new EnemyData();
        var src = Data["Basic Goose 1"];
        foreach (var f in typeof(EnemyData).GetFields()) f.SetValue(withProjectile, f.GetValue(src));
        withProjectile.name = "Basic Goose 1";
        withProjectile.projectilePrefab = new EnemyProjectile { speed = 2.5f };
        spawner.Spawn(withProjectile, SlotAngle(0), false, false, 0, 4f);
        Run(9f);
        Check("Projectile flies to the planet and deals damage on impact (pooled)", Tower(0).Taken > 0 && MonoBehaviour.LiveCount<EnemyProjectile>() <= 2,
            "tower damage " + Tower(0).Taken + ", projectile instances ever made " + MonoBehaviour.LiveCount<EnemyProjectile>() + " for " + Tower(0).Taken + " shots, in flight now " + MonoBehaviour.ActiveCount<EnemyProjectile>());
    }

    static void PhysicsAndPooling()
    {
        // Movement goes through the Rigidbody2D, not the transform
        NewWorld(0);
        var g = Spawn("Basic Goose 2", SlotAngle(0));
        Run(6f);
        Vector2 expected = PlanetMath.ToLocal(SlotAngle(0), Data["Basic Goose 2"].stopDistance, planet.radius);
        var b = g.Body;
        float err = new Vector2(b.position.x - expected.x, b.position.y - expected.y).magnitude;
        Check("Enemies move through a kinematic Rigidbody2D (MovePosition / MoveRotation), not the transform",
            b.bodyType == RigidbodyType2D.Kinematic && b.useFullKinematicContacts && b.gravityScale == 0f && b.MovePositionCalls > 50 && err < 0.01f
            && g.transform.localPosition.sqrMagnitude == 0f && Math.Abs(b.rotation - (SlotAngle(0) + 90f)) < 0.01f,
            "MovePosition calls " + b.MovePositionCalls + ", body at (" + b.position.x.ToString("F1") + ", " + b.position.y.ToString("F1") + ") expected (" + expected.x.ToString("F1") + ", " + expected.y.ToString("F1")
            + "), transform untouched: " + (g.transform.localPosition.sqrMagnitude == 0f) + ", rotation " + b.rotation + " (faces the planet)");

        // Collisions: OnTriggerEnter2D / OnCollisionEnter2D find an IEnemyHitter on the other collider
        NewWorld();
        var target = Spawn("Basic Skunk 3");
        var bullet = new FakeBullet { Damage = 3f };
        var bulletCollider = new Collider2D { attached = bullet };
        Invoke(target, "OnTriggerEnter2D", bulletCollider);
        Invoke(target, "OnCollisionEnter2D", new Collision2D { collider = bulletCollider });
        bool damaged = target.Health == 2f && bullet.Hits == 2;
        var sneaky = Spawn("Stealth Goose");
        Invoke(sneaky, "OnTriggerEnter2D", bulletCollider);
        bool stealthSafe = sneaky.IsAlive && bullet.Hits == 2;
        var radar = new FakeBullet { Damage = 5f, CanHitStealth = true };
        Invoke(sneaky, "OnTriggerEnter2D", new Collider2D { attached = radar });
        bool radarKilled = !sneaky.IsAlive; // read now: the next Spawn may reuse this pooled instance
        var shielded = Spawn("Shielded Skunk");
        Invoke(shielded, "OnTriggerEnter2D", new Collider2D { attached = new FakeBullet { Damage = 1f, IgnoresShield = true } });
        var plain = Spawn("Basic Goose 1");
        Invoke(plain, "OnTriggerEnter2D", new Collider2D()); // something that isn't a hitter
        Check("Collisions and triggers with an IEnemyHitter damage the enemy",
            damaged && stealthSafe && radarKilled && shielded.HasShield && shielded.Health == 7f && plain.IsAlive && plain.Health == 1f,
            "skunk HP 8 -> " + target.Health + " after a trigger + a collision of 3, bullet notified " + bullet.Hits + "x; stealth goose ignored by a normal bullet: " + stealthSafe
            + ", killed by radar: " + radarKilled + "; pest-trap style hit kept the shield: " + shielded.HasShield + "; non-hitter contact did nothing: " + (plain.Health == 1f));

        // Pool: a dead enemy goes back and comes out fresh
        NewWorld(0);
        var deer = Spawn("Basic Deer", SlotAngle(0));
        Run(9f); // orbiting / charging
        int firstId = deer.SpawnId;
        deer.TakeDamage(999f);
        bool inactiveAfterDeath = !deer.gameObject.activeSelf && !BaseEnemyData.Active.Contains(deer);
        var again = Spawn("Basic Deer", 30f);
        bool reused = ReferenceEquals(again, deer);
        bool fresh = again.IsAlive && again.Health == 10f && again.Distance == 4f && !again.Arrived && again.SpawnId != firstId && again.gameObject.activeSelf && BaseEnemyData.Active.Contains(again);
        Run(1f);
        bool notCharging = again.Distance > 3f; // still flying in, not stuck in an old charge
        string doubleRelease = "none";
        try { again.Despawn(); again.Despawn(); } catch (Exception ex) { doubleRelease = ex.GetType().Name; }
        Check("Object pool: a killed enemy is reused fresh (no Instantiate per spawn)",
            inactiveAfterDeath && reused && fresh && notCharging && doubleRelease == "none" && MonoBehaviour.LiveCount<DeerEnemy>() == 1,
            "inactive after death: " + inactiveAfterDeath + ", same instance reused: " + reused + ", fresh HP/distance/state: " + fresh + " (distance after 1s " + again.Distance.ToString("F2")
            + "), double despawn error: " + doubleRelease + ", deer instances " + MonoBehaviour.LiveCount<DeerEnemy>());

        // One PoolManager for everything: enemies and towers (any GameEntity) share it
        NewWorld();
        var towerPrefab = new FakeTowerEntity();
        var t1 = pool.Spawn(towerPrefab, planet.Root);
        t1.Despawn();
        var t2 = pool.Spawn(towerPrefab, planet.Root);
        var goose = Spawn("Basic Goose 1");
        int managers = MonoBehaviour.LiveCount<PoolManager>();
        bool spawnerHasNoPools = typeof(EnemySpawner).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            .All(f => !f.FieldType.Name.StartsWith("ObjectPool") && !(f.FieldType.IsGenericType && f.FieldType.GetGenericArguments().Any(a => a.Name.StartsWith("ObjectPool"))));
        Check("One PoolManager serves enemies and towers (GameEntity base); the spawner owns no pools",
            managers == 1 && ReferenceEquals(t1, t2) && t2.SpawnedCalls == 2 && t2.DespawnedCalls == 1 && t2.IsSpawned && goose is GameEntity && goose.SourcePrefab != null
            && pool.CountAll(towerPrefab) == 1 && pool.CountActive(towerPrefab) == 1 && spawnerHasNoPools,
            "PoolManagers in level " + managers + ", tower instance reused: " + ReferenceEquals(t1, t2) + " (OnSpawned " + t2.SpawnedCalls + ", OnDespawned " + t2.DespawnedCalls
            + "), goose is a GameEntity from the same manager: " + (goose.SourcePrefab != null) + ", spawner owns no ObjectPool: " + spawnerHasNoPools);

        // Endangered Deer: a guard that died and was reused as a normal deer no longer counts
        NewWorld(0);
        var ed = (EndangeredDeerBoss)Spawn("Endangered Deer", SlotAngle(0), false, true);
        Run(6f);
        var guard = BaseEnemyData.Active.First(e => e.name == "Shielded Deer");
        guard.TakeDamage(1f); guard.TakeDamage(999f);
        var reusedGuard = Spawn("Shielded Deer", 200f); // same pooled instance, now an unrelated deer
        int living = ed.LivingGuards;
        Check("Pooled guards: a reused instance isn't mistaken for the boss's old guard",
            ReferenceEquals(reusedGuard, guard) && living == 0, "reused same instance: " + ReferenceEquals(reusedGuard, guard) + ", boss counts living guards " + living);
    }

    static void Invoke(BaseEnemyData e, string method, object arg)
    {
        typeof(BaseEnemyData).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(e, new[] { arg });
    }

    // README table vs EnemyDataGenerator table
    static void ReadmeMatchesGenerator(string readmePath)
    {
        string gen = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../Assets/Editor/Enemies/EnemyDataGenerator.cs"));
        var genRows = Regex.Matches(gen, @"new Row\(""([^""]+)"",\s*([\d.]+),\s*(\d+),\s*(\d+),\s*([\d.]+),\s*EnemyAttackType\.(\w+)")
            .Cast<Match>().ToDictionary(m => m.Groups[1].Value, m => m);
        if (readmePath == null || !File.Exists(readmePath))
        {
            Check("Generator table matches the README", false, "README not found (pass its path as the first argument)");
            return;
        }
        var bad = new List<string>(); int checkedRows = 0;
        foreach (var line in File.ReadAllLines(readmePath))
        {
            var cells = line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length < 6) continue;
            // Enemies: Name | HP | Money | Fuel | Damage | Attack | ...   Bosses: Level | Name | HP | Orbit | Money | Fuel | Damage | Attack | ...
            string name; string hp, money, fuel, dmg, atk;
            if (genRows.ContainsKey(cells[0])) { name = cells[0]; hp = cells[1]; money = cells[2]; fuel = cells[3]; dmg = cells[4]; atk = cells[5]; }
            else if (cells.Length >= 8 && genRows.ContainsKey(cells[1])) { name = cells[1]; hp = cells[2]; money = cells[4]; fuel = cells[5]; dmg = cells[6]; atk = cells[7]; }
            else continue;
            checkedRows++;
            var g = genRows[name];
            string gAtk = g.Groups[6].Value;
            string readmeAtk = atk.Equals("AOE", StringComparison.OrdinalIgnoreCase) ? "AoE" : atk.StartsWith("N/A") ? "None" : "Single";
            float rd; bool dmgNumeric = float.TryParse(dmg, out rd);
            bool ok = g.Groups[2].Value == hp && g.Groups[3].Value == money && g.Groups[4].Value == fuel && gAtk == readmeAtk
                      && (dmgNumeric ? float.Parse(g.Groups[5].Value) == rd : true);
            if (!ok) bad.Add(name);
        }
        Check("Generator table matches the README (HP, money, fuel, damage, attack type)", checkedRows == 19 && bad.Count == 0,
            checkedRows + " of 19 README rows checked" + (bad.Count > 0 ? ", mismatched: " + string.Join(", ", bad) : ", all match"));
    }
}
