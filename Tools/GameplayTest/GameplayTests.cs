// End-to-end gameplay tests without Unity: LevelManager + LevelPlanetDefense + towers + enemies + PoolManager + AI.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EnemyAI;

class PlanetObject : MonoBehaviour { }
/// <summary>A planet that is UI on a Screen Space - Overlay canvas: its world space is screen pixels.</summary>
class OverlayPlanet : Canvas { }

static class GameplayTests
{
    static int pass, fail;

    static void Check(string name, bool ok, string detail)
    {
        if (ok) pass++; else fail++;
        Console.WriteLine((ok ? "PASS " : "FAIL ") + name + " -> " + detail);
    }

    static PoolManager pool;
    static PlanetObject planet;
    static LevelManager lm;
    static LevelPlanetDefense defense;
    static LevelData level;
    static GameManager game;
    static readonly Dictionary<string, EnemyData> E = new Dictionary<string, EnemyData>();
    static readonly Dictionary<string, TowerData> T = new Dictionary<string, TowerData>();

    static EnemyData Enemy(string name, float hp, int money, float dmg, EnemyAttackType a, bool shield = false, bool stealth = false, int cost = 3)
    {
        var d = new EnemyData { name = name, maxHealth = hp, moneyDrop = money, fuelDrop = money, damage = dmg, attackType = a, hasShield = shield, isStealth = stealth,
            cost = cost, attackInterval = 1.5f, movement = EnemyMovement.StopAndShoot, prefab = new BasicEnemy() };
        E[name] = d;
        return d;
    }

    static TowerData Tower(string name, float hp, int cost, int fuel, float fov, float dmg, float rate, TowerAttackType a, BaseTowerData prefab, TowerKind kind,
                           bool projectile = true, bool ignoresShield = false, bool seesStealth = false)
    {
        var t = new TowerData { name = name, maxHealth = hp, cost = cost, fuelIntake = fuel, fov = fov, damage = dmg, fireRate = rate, attackType = a, prefab = prefab, kind = kind,
            ignoresShield = ignoresShield, canHitStealth = seesStealth, projectilePrefab = projectile ? new TowerProjectile { speed = 5f } : null };
        T[name] = t;
        return t;
    }

    static void BuildData()
    {
        Enemy("Basic Goose 1", 1, 1, 1, EnemyAttackType.Single);
        Enemy("Basic Goose 3", 4, 1, 1, EnemyAttackType.Single, cost: 6);
        Enemy("Shielded Goose", 4, 1, 1, EnemyAttackType.Single, shield: true, cost: 8);
        Enemy("Stealth Goose", 1, 1, 1, EnemyAttackType.Single, stealth: true, cost: 5);
        Enemy("Basic Skunk 1", 2, 2, 2, EnemyAttackType.AoE, cost: 6);
        E["Basic Skunk 1"].attackInterval = 2f;
        var boss = Enemy("Healthy Goose", 50, 10, 2, EnemyAttackType.Single, cost: 40);
        boss.isBoss = true; boss.movement = EnemyMovement.Orbit; boss.prefab = new BossEnemy();

        // README tower table (fire rates are not in the README)
        Tower("Laser", 25, 1, -1, 120, 2, 1f, TowerAttackType.Single, new ShootingTower(), TowerKind.Laser);
        Tower("Bomb", 25, 2, -1, 120, 1, 1.5f, TowerAttackType.AoE, new AoETower { effectRadius = 1f }, TowerKind.Bomb);
        Tower("Pest Trap", 20, 2, -2, 120, 1, 2f, TowerAttackType.Dot, new PestTrapTower(), TowerKind.PestTrap, ignoresShield: true);
        Tower("Radar", 10, 5, -1, 90, 0, 0.5f, TowerAttackType.None, new RadarTower(), TowerKind.Radar, projectile: false, seesStealth: true);
        Tower("Oil Rig", 15, 5, 3, 0, 0, 1f, TowerAttackType.None, new ResourceTower { resourceType = ResourceTower.ResourceType.Fuel, generationAmount = 0 }, TowerKind.OilRig, projectile: false);
        Tower("Bank", 15, 5, -1, 0, 0, 1f, TowerAttackType.None, new ResourceTower { resourceType = ResourceTower.ResourceType.Money, generationAmount = 3 }, TowerKind.Bank, projectile: false);
        Tower("Ballistica", 30, 25, -6, 120, 0, 2f, TowerAttackType.Single, new BallisticaTower(), TowerKind.Ballistica);
    }

    static void NewWorld(int money = 50, float fuel = 50, float rotation = 0f)
    {
        MonoBehaviour.ClearAll();
        BaseEnemyData.Active.Clear();
        BaseTowerData.Active.Clear();
        MonoBehaviour.StubPhysics = true;
        UnityEngine.Random.Rng = new System.Random(3);
        UnityEngine.Debug.Logs.Clear();
        Time.timeScale = 1f;

        game = new GameManager();
        typeof(GameManager).GetProperty("Instance").SetValue(null, null);
        MonoBehaviour.Register(game);
        pool = new PoolManager();
        MonoBehaviour.Register(pool);
        planet = new PlanetObject();
        MonoBehaviour.Register(planet);
        planet.AddCircle(2f, false); // planet radius 2 world units
        level = new LevelData { money = money, fuel = fuel, planetMaxHealth = 100, planetRotationSpeed = rotation, isClockwise = true, maxTime = 60,
            startingDifficulty = 10, postBossDifficulty = 20, maxDifficulty = 60, enemyDataList = new List<LevelEnemyEntry>() };
        lm = new LevelManager { planetBody = planet.GetComponent<Rigidbody2D>() };
        typeof(LevelManager).GetField("levelData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(lm, level);
        MonoBehaviour.Register(lm);
        defense = new LevelPlanetDefense { levelManager = lm, slots = 12 };
        MonoBehaviour.Register(defense);
    }

    static BaseEnemyData SpawnEnemy(string name, float angle, float distance = 4f)
    {
        var e = pool.Spawn(E[name].prefab, defense.Root);
        e.name = name;
        e.Initialize(E[name], new EnemySpawnContext { Planet = defense, AngleDegrees = angle, SpawnDistance = distance });
        return e;
    }

    static BaseTowerData Place(string tower, int slot)
    {
        string reason;
        bool ok = defense.TryPlaceTower(T[tower], slot, out reason);
        if (!ok) Console.WriteLine("   (place " + tower + " failed: " + reason + ")");
        return defense.TowerIn(slot);
    }

    static void Run(float seconds, float dt = 0.02f) { for (float t = 0; t < seconds; t += dt) MonoBehaviour.Frame(dt); }

    static void Main()
    {
        Console.WriteLine("=== Gameplay tests (towers + enemies + LevelManager)");
        BuildData();

        // 1. building spends money, tower sits on its slot facing outward, rides the rotation
        NewWorld(money: 10, rotation: 30f);
        var laser = Place("Laser", 0);
        float moneyAfterBuild = lm.TotalMoney;
        Run(0.1f);
        Vector2 p0 = laser.Body.position;
        float r0 = p0.magnitude;
        Run(2f);
        Vector2 p1 = laser.Body.position;
        float turned = Vector2.Angle(p0, p1);
        Vector2 outward = laser.transform.up;
        float facing = Vector2.Angle(outward, p1);
        Check("Building a tower spends its cost; it sits on the planet, faces outward and turns with the planet",
            laser != null && moneyAfterBuild == 9 && Math.Abs(r0 - 2.2f) < 0.05f && turned > 40f && turned < 80f && facing < 1f && laser.IsSpawned,
            "money 10 -> " + moneyAfterBuild + " (Laser $1), distance from centre " + r0.ToString("F2") + " (radius 2, towers at 1.1 radii), turned " + turned.ToString("F0") + " deg in 2s at 30 deg/s, facing error " + facing.ToString("F1") + " deg");

        // 2. not enough money / occupied slot
        NewWorld(money: 1);
        Place("Laser", 0);
        string why1, why2;
        bool second = defense.TryPlaceTower(T["Laser"], 1, out why1);
        lm.ChangeMoney(10);
        bool occupied = defense.TryPlaceTower(T["Laser"], 0, out why2);
        Check("Can't build without the money or on an occupied slot", !second && !occupied && lm.TotalMoney == 10, "broke: \"" + why1 + "\", occupied: \"" + why2 + "\", money " + lm.TotalMoney);

        // 3. laser shoots an enemy in its FOV with a pooled projectile; kill pays the drop
        NewWorld(money: 10);
        Place("Laser", 0); // slot 0 = top (90 deg)
        int moneyBefore = lm.TotalMoney;
        var goose = SpawnEnemy("Basic Goose 3", 90f);
        int gooseId = goose.SpawnId;
        Run(6f);
        bool killed = !goose.IsAlive || goose.SpawnId != gooseId;
        Check("Laser kills a goose in its FOV with projectiles; the kill pays money", killed && lm.TotalMoney == moneyBefore + 1 && MonoBehaviour.LiveCount<TowerProjectile>() <= 3,
            "goose (HP 4, laser 2 dmg) killed: " + killed + ", money " + moneyBefore + " -> " + lm.TotalMoney + ", projectile instances made " + MonoBehaviour.LiveCount<TowerProjectile>());

        // 4. FOV is an angle: an enemy on the far side of the planet is ignored
        NewWorld(money: 10);
        Place("Laser", 0);
        var below = SpawnEnemy("Basic Goose 1", 270f);
        Run(6f);
        Check("FOV is an angle (README 120 deg): an enemy behind the planet isn't targeted", below.IsAlive && MonoBehaviour.LiveCount<TowerProjectile>() == 0,
            "enemy below alive: " + below.IsAlive + ", shots fired " + MonoBehaviour.LiveCount<TowerProjectile>());

        // 5. enemies hit the tower in the slot below, or the planet's HP in LevelManager
        NewWorld(money: 10, fuel: 0); // no fuel: towers are disabled and don't shoot back
        var target = Place("Laser", 0);
        lm.ChangeFuel(0);
        SpawnEnemy("Basic Goose 3", 90f);
        SpawnEnemy("Basic Goose 3", 180f);
        Run(12f);
        Check("Enemy shots hit the tower in the slot below, or the planet HP (LevelManager) over an empty slot",
            target.Health < 25f && lm.PlanetHP < 100f && lm.PlanetHP > 80f,
            "tower HP 25 -> " + target.Health + ", planet HP 100 -> " + lm.PlanetHP + " (ReceiveDamage returns the damage dealt for the AI)");

        // 6. selling refunds half and the tower goes back to the pool
        NewWorld(money: 10);
        var bomb = Place("Bomb", 3);
        int afterBuy = lm.TotalMoney;
        bool sold = defense.SellTower(3);
        var again = Place("Bomb", 5);
        Check("Selling refunds half the cost and the pooled tower is reused", sold && afterBuy == 8 && ReferenceEquals(bomb, again) && defense.TowerIn(3) == null && defense.TowerIn(5) == again,
            "money 10 -> " + afterBuy + " (Bomb $2) -> sold +1 -> rebuilt: " + lm.TotalMoney + ", same instance: " + ReferenceEquals(bomb, again));

        // 7. economy: Bank $3/s, Oil Rig +3 F/s, laser -1 F/s
        NewWorld(money: 20, fuel: 10);
        Place("Bank", 0); Place("Oil Rig", 1); Place("Laser", 2);
        float m0 = lm.TotalMoney, f0 = lm.TotalFuel;
        Run(5f);
        float dm = lm.TotalMoney - m0, df = lm.TotalFuel - f0;
        Check("Economy: Bank makes $3/s, Oil Rig +3 fuel/s, Laser and Bank use 1 fuel/s each", dm >= 12 && dm <= 15 && df >= 3 && df <= 5,
            "in 5s money +" + dm + " (expected ~15), fuel " + (df >= 0 ? "+" : "") + df + " (expected +3-2 = +1/s -> ~5)");

        // 8. Pest Trap: slow + 1 dmg/s for 3 s that ignores the shield (projectile itself deals 0)
        NewWorld(money: 10);
        Place("Pest Trap", 0);
        var shielded = SpawnEnemy("Shielded Goose", 90f);
        Run(4.5f);
        float hpMid = shielded.Health;
        Run(4f);
        Check("Pest Trap: damage over time ignores the shield (README 1/s for 3s)", shielded.HasShield && hpMid < 4f,
            "shield kept: " + shielded.HasShield + ", HP 4 -> " + hpMid.ToString("F2") + " -> " + (shielded.IsAlive ? shielded.Health.ToString("F2") : "dead"));

        // 9. Radar reveals stealth so a laser can shoot it
        NewWorld(money: 20);
        Place("Laser", 1);
        var sneaky = SpawnEnemy("Stealth Goose", 105f);
        int sneakyId = sneaky.SpawnId;
        Run(5f);
        bool aliveWithoutRadar = sneaky.IsAlive && sneaky.SpawnId == sneakyId;
        Place("Radar", 0);
        Run(5f);
        bool deadWithRadar = !sneaky.IsAlive || sneaky.SpawnId != sneakyId;
        Check("Stealth goose: lasers can't see it until a Radar reveals it", aliveWithoutRadar && deadWithRadar, "alive with laser only: " + aliveWithoutRadar + ", killed after radar: " + deadWithRadar);

        // 10. Ballistica: 20% of max HP per hit
        NewWorld(money: 30);
        Place("Ballistica", 0);
        var big = SpawnEnemy("Basic Goose 3", 90f);
        Run(4.5f);
        float lost = 4f - big.Health; float hits = lost / 0.8f;
        Check("Ballistica deals 20% of the enemy's max HP per hit", big.Health < 4f && Math.Abs(hits - Math.Round(hits)) < 0.01f,
            "HP 4 -> " + big.Health.ToString("F2") + " = " + Math.Round(hits) + " hits of 0.8");

        // 11. Davis Nova: towers have a lifespan, flash, then explode (damaging nearby enemies) and are destroyed
        NewWorld(money: 20);
        var boom = new PooledEffect { lifetime = 0.5f };
        var life = new TowerLifespan { lifespanSeconds = 3f, warningSeconds = 1f, explosionRadius = 2f, explosionDamage = 3f, explosionEffect = boom };
        MonoBehaviour.Register(life);
        T["Bank"].prefab.AddArt();
        var bank = Place("Bank", 0);
        int bankId = bank.SpawnId;
        var victim = SpawnEnemy("Basic Goose 3", 90f, 2.0f); // close in: stops 1.6 units above the tower
        var far = SpawnEnemy("Basic Goose 3", 270f);     // other side of the planet
        int destroyed = 0; Action<BaseTowerData> onDestroyed = t => destroyed++;
        TowerEvents.TowerDestroyed += onDestroyed;
        Run(2.5f);
        var artNow = bank.GetComponentsInChildren<SpriteRenderer>();
        bool flashing = artNow.Length > 0 && (artNow[0].color.g < 0.99f || artNow[0].color.b < 0.99f);
        float leftBefore = life.TimeLeft(bank);
        Run(0.7f);
        TowerEvents.TowerDestroyed -= onDestroyed;
        bool gone = defense.TowerIn(0) == null && (!bank.IsAlive || bank.SpawnId != bankId);
        int effects = MonoBehaviour.ActiveCount<PooledEffect>();
        Run(1f);
        Check("Davis Nova: a tower flashes near the end of its lifespan, explodes (hurts nearby enemies) and is destroyed",
            flashing && gone && destroyed == 1 && Math.Abs(victim.Health - 1f) < 0.01f && far.Health == 4f && effects == 1 && MonoBehaviour.ActiveCount<PooledEffect>() == 0,
            "flashing red in the last second: " + flashing + " (" + leftBefore.ToString("F1") + "s left), destroyed: " + gone + " (TowerDestroyed x" + destroyed + "), nearby goose HP 4 -> " + victim.Health
            + ", far goose HP " + far.Health + ", pooled explosion shown then returned: " + (effects == 1 && MonoBehaviour.ActiveCount<PooledEffect>() == 0));

        // 12. Silverman Luna: the moon orbits and spins; its 4 slots carry towers; they count for the AI
        NewWorld(money: 20);
        var moonObj = new PlanetObject();
        moonObj.transform.position = new Vector3(3f, 0f, 0f); // authored 1.5 planet radii to the right
        MonoBehaviour.Register(moonObj);
        moonObj.AddCircle(0.5f, false);
        var moon = new MoonOrbit { planet = defense, moon = moonObj.transform, orbitDegreesPerSecond = 30f, spinDegreesPerSecond = -45f };
        MonoBehaviour.Register(moon);
        defense.moon = moon;
        Run(0.1f);
        string moonWhy;
        bool builtOnMoon = moon.TryPlaceTower(T["Laser"], 0, out moonWhy);
        var moonTower = moon.TowerIn(0);
        Vector2 mp0 = moonObj.GetComponent<Rigidbody2D>().position;
        Run(2f);
        Vector2 mp1 = moonObj.GetComponent<Rigidbody2D>().position;
        float orbitTurn = Vector2.Angle(mp0, mp1);
        float towerToMoon = Vector2.Distance(moonTower.Body.position, moon.Center);
        int slotsFull = 0; string w;
        for (int i = 1; i < 4; i++) if (moon.TryPlaceTower(T["Laser"], i, out w)) slotsFull++;
        bool fifth = moon.SlotCount == 4;
        int aiSees = defense.BuildSnapshot().Towers.Count;
        Check("Silverman Luna: the moon orbits at its authored distance and spins; towers ride its 4 slots and count for the AI",
            builtOnMoon && Math.Abs(mp1.magnitude - 3f) < 0.02f && orbitTurn > 50f && orbitTurn < 70f && Math.Abs(towerToMoon - 0.5f) < 0.02f && Math.Abs(moon.SpinDegrees - (360f - 45f * 2.1f)) < 4f
            && slotsFull == 3 && fifth && aiSees == 4 && lm.TotalMoney == 16,
            "built on moon: " + builtOnMoon + (moonWhy != null ? " (" + moonWhy + ")" : "") + ", orbit radius " + mp1.magnitude.ToString("F2") + " (authored 3.00), turned " + orbitTurn.ToString("F0")
            + " deg in 2s at 30/s, spin " + moon.SpinDegrees.ToString("F0") + ", tower " + towerToMoon.ToString("F2") + " from the moon centre (moon radius 0.5), 4 slots filled, AI sees " + aiSees + " towers, money 20 -> " + lm.TotalMoney);

        // 13. the planet and moon are UI on an overlay canvas (pixels); enemies/towers are in the camera's world
        NewWorld(money: 20);
        var cam = new Camera { orthographicSize = 5f };   // 1080 px high -> 108 px per world unit, screen centre = (0,0)
        MonoBehaviour.Register(cam);
        Camera.main = cam;
        var uiPlanet = new OverlayPlanet { renderMode = RenderMode.ScreenSpaceOverlay };
        uiPlanet.transform.position = new Vector3(960f + 216f, 540f, 0f); // 2 world units right of centre
        MonoBehaviour.Register(uiPlanet);
        uiPlanet.AddCircle(216f, false);                                // 216 px = 2 world units
        var uiDefense = new LevelPlanetDefense { levelManager = lm, slots = 12, planet = uiPlanet.transform };
        MonoBehaviour.Register(uiDefense);
        var uiMoonObj = new OverlayPlanet { renderMode = RenderMode.ScreenSpaceOverlay };
        uiMoonObj.transform.position = new Vector3(960f + 216f + 432f, 540f, 0f); // 2 planet radii right of the planet
        MonoBehaviour.Register(uiMoonObj);
        uiMoonObj.AddCircle(54f, false);                                   // 0.5 world units
        var uiMoon = new MoonOrbit { planet = uiDefense, moon = uiMoonObj.transform, orbitDegreesPerSecond = 0f, spinDegreesPerSecond = 0f };
        MonoBehaviour.Register(uiMoon);
        Run(0.1f);
        string uiWhy, uiWhy2;
        bool uiPlaced = uiDefense.TryPlaceTower(T["Laser"], 0, out uiWhy);
        bool uiMoonPlaced = uiMoon.TryPlaceTower(T["Laser"], 0, out uiWhy2);
        Run(0.1f);
        Vector2 top = uiDefense.TowerIn(0).Body.position;
        Vector2 moonTop = uiMoon.TowerIn(0).Body.position;
        bool moonIsTrigger = uiMoonObj.GetComponent<Collider2D>().isTrigger;
        Camera.main = null;
        Check("Overlay-canvas planet: UI pixels are converted to the camera's world for towers (and the moon's towers)",
            uiPlaced && uiMoonPlaced && Math.Abs(uiDefense.Radius - 2f) < 0.01f && Vector2.Distance(uiDefense.Root.position, new Vector2(2f, 0f)) < 0.01f
            && Vector2.Distance(top, new Vector2(2f, 2.2f)) < 0.02f && Vector2.Distance(moonTop, new Vector2(6f, 0.5f)) < 0.02f && moonIsTrigger,
            "planet radius " + uiDefense.Radius.ToString("F2") + " world (216 px), centre " + ((Vector2)uiDefense.Root.position).x.ToString("F2") + "," + ((Vector2)uiDefense.Root.position).y.ToString("F2")
            + ", top tower at " + top.x.ToString("F2") + "," + top.y.ToString("F2") + " (expect 2,2.2), moon tower at " + moonTop.x.ToString("F2") + "," + moonTop.y.ToString("F2") + " (expect 6,0.5), moon collider is a trigger: " + moonIsTrigger);

        // 14. full level through the spawner and the AI with towers; level complete after time up
        NewWorld(money: 60, fuel: 600);
        foreach (var n in new[] { "Basic Goose 1", "Basic Goose 3", "Shielded Goose", "Stealth Goose", "Basic Skunk 1" })
            level.enemyDataList.Add(new LevelEnemyEntry { enemy = E[n], startingBudget = 1, endingBudget = 1 });
        level.enemyDataList.Add(new LevelEnemyEntry { enemy = E["Healthy Goose"], isLevelBoss = true, startingBudget = 1, endingBudget = 1 });
        var bridge = new WaveDirectorBridge { seed = 2 };
        MonoBehaviour.Register(bridge);
        var spawner = new EnemySpawner { level = level, bridge = bridge, planetDefense = defense, startOnAwake = false, spawnDistance = 4f };
        MonoBehaviour.Register(spawner);
        defense.spawner = spawner;
        foreach (int s in new[] { 0, 2, 4, 6, 8, 10 }) Place("Laser", s);
        Place("Bomb", 3); Place("Oil Rig", 9);
        Place("Radar", 1); Place("Radar", 5); Place("Radar", 7); Place("Radar", 11); // without radar the AI learns to send only stealth geese
        int kills = 0; Action<int, int> onKill = (m, f) => kills++;
        EnemyEvents.Killed += onKill;
        string error = null;
        try
        {
            spawner.Begin();
            int spawned = 0; Action<BaseEnemyData> onSp = e => spawned++; EnemyEvents.Spawned += onSp;
            for (float t = 0; t < 120 && game.CurrentState != GameManager.GameState.LevelComplete && game.CurrentState != GameManager.GameState.GameOver; t += 0.02f)
            {
                MonoBehaviour.Frame(0.02f);
                if (Environment.GetEnvironmentVariable("DEBUGLEVEL") != null && Math.Abs(t % 5f) < 0.02f)
                    Console.WriteLine("  t=" + t.ToString("F0") + " hp=" + lm.PlanetHP + " spawned=" + spawned + " kills=" + kills + " active=" + string.Join(",", BaseEnemyData.Active.GroupBy(e => e.name).Select(g => g.Key + ":" + g.Count()))
                        + " dist=" + string.Join(",", BaseEnemyData.Active.Take(4).Select(e => e.Distance.ToString("F1") + "@" + e.AngleDegrees.ToString("F0") + (e.IsTargetable(false) ? "" : "(hidden)")))
                        + " proj=" + MonoBehaviour.ActiveCount<TowerProjectile>() + " towersDisabled=" + BaseTowerData.Active.Count(x => x.IsDisabled));
            }
            EnemyEvents.Spawned -= onSp;
        }
        catch (Exception ex) { error = ex.GetType().Name + ": " + ex.Message + " " + ex.StackTrace.Split('\n')[0]; }
        EnemyEvents.Killed -= onKill;
        var errs = UnityEngine.Debug.Logs.Where(l => l.StartsWith("ERROR")).ToList();
        Check("Full 60s level: spawner + AI + towers + LevelManager, then LevelComplete", error == null && errs.Count == 0 && kills > 5
            && game.CurrentState == GameManager.GameState.LevelComplete,
            (error ?? "no exceptions") + (errs.Count > 0 ? ", errors: " + string.Join(" | ", errs) : "") + ", kills " + kills + ", state " + game.CurrentState
            + ", planet HP " + lm.PlanetHP.ToString("F0") + ", money " + lm.TotalMoney + ", fuel " + lm.TotalFuel.ToString("F0") + ", towers alive " + BaseTowerData.Active.Count);

        Console.WriteLine("Gameplay: " + pass + " passed, " + fail + " failed");
        if (fail > 0) Environment.ExitCode = 1;
    }
}
