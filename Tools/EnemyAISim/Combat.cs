using System;
using System.Collections.Generic;
using System.Linq;
using EnemyAI;

// Simple 2D combat model built from the README numbers, so the player's weakness comes out of
// the tower loadout instead of being assumed. Everything the README doesn't say is an
// ASSUMPTION and lives in CombatAssumptions, so it can be swapped once the real game exists.
static class CombatAssumptions
{
    public const float Dt = 0.2f;              // simulation step (s)
    public const int Slots = 12;               // medium planet (README: medium = 12 slots)
    public const float RotationDegPerSec = 6f; // "normal" rotation, clockwise
    public const float SpawnRadius = 10f;      // planet radius = 1
    public const float StopRadius = 3f;        // where geese / skunks stop and shoot
    public const float OrbitRadius = 3.5f;     // deer / orbiting enemies
    public static float TowerRange = 5f;       // towers engage enemies closer than this (calibrated: see README)
    public const float ApproachSpeed = 1.5f;   // units/s
    public const float OrbitDegPerSec = 15f;
    public const float AoeRadiusDeg = 15f;     // AoE hits enemies within this angle of the target
    public const float StartingFuel = 60f;
    public static int MaxDifficulty = 800;     // level cost budget, calibrated so a random enemy mix destroys ~3 of 12 towers

    // Shots per second. README only says "Sniper: lower fire rate", "Laser SMG: faster rate".
    public static float FireRate(TowerKind k)
    {
        switch (k)
        {
            case TowerKind.Sniper: return 0.4f;
            case TowerKind.LaserSMG: return 2.5f;
            case TowerKind.Ballistica: return 0.5f;
            case TowerKind.PestTrap: return 0.5f;
            default: return 1f;
        }
    }

    // Seconds between enemy attacks. README gives damage, not timing.
    public static float AttackCooldown(EnemyKind k)
    {
        if (EnemyCatalog.IsSkunk(k)) return 2f;
        if (EnemyCatalog.IsDeer(k)) return 4f;   // the periodic "charge"
        return 1.5f;
    }
}

static class Readme
{
    // From README: (HP, damage, AoE)
    public static void Enemy(EnemyKind k, out float hp, out float dmg, out bool aoe)
    {
        switch (k)
        {
            case EnemyKind.BasicGoose1: hp = 1; dmg = 1; aoe = false; return;
            case EnemyKind.BasicGoose2: hp = 2; dmg = 1; aoe = false; return;
            case EnemyKind.BasicGoose3: hp = 4; dmg = 1; aoe = false; return;
            case EnemyKind.ShieldedGoose: hp = 4; dmg = 1; aoe = false; return;
            case EnemyKind.StealthGoose: hp = 1; dmg = 1; aoe = false; return;
            case EnemyKind.BasicSkunk1: hp = 2; dmg = 2; aoe = true; return;
            case EnemyKind.BasicSkunk2: hp = 4; dmg = 2; aoe = true; return;
            case EnemyKind.BasicSkunk3: hp = 8; dmg = 2; aoe = true; return;
            case EnemyKind.ShieldedSkunk: hp = 8; dmg = 2; aoe = true; return;
            case EnemyKind.BasicDeer: hp = 10; dmg = 5; aoe = false; return;
            case EnemyKind.ShieldedDeer: hp = 10; dmg = 5; aoe = false; return;
            default: hp = 10; dmg = 1; aoe = false; return;
        }
    }

    // From README: fuel dropped on death (shiny = double).
    public static float FuelDrop(EnemyKind k)
    {
        if (EnemyCatalog.IsSkunk(k)) return 2;
        if (EnemyCatalog.IsDeer(k)) return 4;
        return 1;
    }

    // From README: (HP, damage, fuel per second). Ballistica damage is 20% of the target's max HP.
    public static void Tower(TowerKind k, out float hp, out float dmg, out float fuel)
    {
        switch (k)
        {
            case TowerKind.Laser: hp = 25; dmg = 2; fuel = -1; return;
            case TowerKind.Bomb: hp = 25; dmg = 1; fuel = -1; return;
            case TowerKind.Sniper: hp = 20; dmg = 5; fuel = -1; return;
            case TowerKind.PestTrap: hp = 20; dmg = 1; fuel = -2; return; // 1/s for 3s + slow
            case TowerKind.Radar: hp = 10; dmg = 0; fuel = -1; return;
            case TowerKind.OilRig: hp = 15; dmg = 0; fuel = 3; return;
            case TowerKind.Bank: hp = 15; dmg = 0; fuel = -1; return;
            case TowerKind.LaserSMG: hp = 30; dmg = 2; fuel = -3; return;
            case TowerKind.TeslaCoil: hp = 25; dmg = 2; fuel = -3; return;
            case TowerKind.GammaRadar: hp = 20; dmg = 1; fuel = -5; return;
            case TowerKind.Ballistica: hp = 30; dmg = 0; fuel = -6; return;
            default: hp = 10; dmg = 0; fuel = 0; return;
        }
    }
}

/// <summary>v1.3 experiment knobs. Defaults = the v1.2 model exactly.</summary>
class CombatOptions
{
    public Fam? Only;                    // send just this family (hindsight best-family reference)
    public double? StealthShare;         // R4a: fix stealth geese at this share of the designer budget (others keep their designer ratios); use with DesignerOnly
    public float FireRateMul = 1f;       // R4b sensitivity: tower fire rates
    public float RangeMul = 1f;          // R4b: tower range
    public float RevealMul = 1f;         // R4b: how wide radar reveals stealth (half-angle multiplier)
    public bool NewFuel;                 // Q4: fuel as in the game since 2026-11-08 (weapons pay per shot; snapshot = estimate at full fire rate)
    public Action<WaveDirectorBridge> Configure; // e.g. learner settings for the cap experiment (R4d)
    public bool Posterior;               // R4c: record the learner's beliefs at the end
}

class CombatResult
{
    public double[] PosteriorMean;     // R4c: learner mean reward per family at the end (G/S/Sk/D)
    public double[] FactorAtCap;       // R4c: share of Thompson draws that hit the learned-factor ceiling
    public double[] DrawMean;          // R4c: mean learned factor of those draws
    public double DamageToPlayer;     // towers + planet, after the first 20% of the level
    public double Spent;              // cost spent after the first 20%
    public double PlanetDamage;
    public int TowersLost;
    public int Kills;
    public double[] FamCost = new double[4];
    public double Pressure { get { return Spent > 0 ? DamageToPlayer / Spent : 0; } }
}

static class Combat
{
    class Tower { public TowerKind Kind; public int Slot; public float Hp; public float Cooldown; public bool Alive = true; }
    class Foe
    {
        public EnemyKind Kind; public int Ticket; public float Angle; public float R; public float Hp, MaxHp; public bool Shield;
        public bool Orbit; public bool Arrived; public float Cooldown; public float Dot; public float DotLeft; public float Slow;
        public bool Stealth; public bool Shiny;
    }

    // 12 slots each, in slot order (null = empty). Each loadout is built so its fuel roughly balances
    // (README: Oil Rig +3/s, Laser/Bomb/Sniper/Radar -1/s, Pest Trap -2/s, Tesla -3/s, Gamma -5/s),
    // like a real player would; kill drops add a little more.
    const TowerKind L = TowerKind.Laser, B = TowerKind.Bomb, Sn = TowerKind.Sniper, R = TowerKind.Radar, O = TowerKind.OilRig,
                    P = TowerKind.PestTrap, Te = TowerKind.TeslaCoil, G = TowerKind.GammaRadar;
    public static readonly Dictionary<string, TowerKind?[]> Loadouts = new Dictionary<string, TowerKind?[]>
    {
        { "Single-target heavy",    new TowerKind?[] { Sn, L, O, L, Sn, R, O, L, Sn, L, O, L } },   // fuel -9 / +9
        { "AoE heavy",              new TowerKind?[] { B, O, B, Te, O, B, G, O, B, L, O, B } },     // fuel -14 / +12
        { "No radar",               new TowerKind?[] { L, Sn, O, L, B, O, L, Sn, O, L, L, L } },   // fuel -9 / +9
        { "Clustered (half empty)", new TowerKind?[] { L, B, O, L, R, O, L, null, null, null, null, null } }, // fuel -5 / +6
        { "Balanced",               new TowerKind?[] { L, B, O, Sn, R, O, L, P, O, L, L, null } },  // fuel -9 / +9
    };

    static float Norm(float a) { a %= 360f; return a < 0 ? a + 360f : a; }

    static LevelData MakeLevel(Variant v, Fam? only, double? stealthShare = null)
    {
        int maxD = CombatAssumptions.MaxDifficulty;
        var lv = new LevelData { maxTime = 180, startingDifficulty = maxD / 10, postBossDifficulty = maxD / 2, maxDifficulty = maxD, enemyDataList = new List<LevelEnemyEntry>() };
        foreach (var kv in EnemyCatalog.DefaultWeight)
        {
            bool deer = EnemyCatalog.IsDeer(kv.Key);
            float s0, s1;
            if (only.HasValue) { s0 = s1 = Bench.FamOfPublic(kv.Key) == only.Value ? 1f : 0f; }
            else if (v == Variant.Random) { s0 = s1 = 1f; }
            else { s0 = deer ? 0.02f : 0.1f; s1 = deer ? 0.1f : 0.08f; }
            if (stealthShare.HasValue) { if (kv.Key == EnemyKind.StealthGoose) { s0 = s1 = -1f; } else if (stealthShare.Value >= 1.0) { s0 = s1 = 0f; } }
            lv.enemyDataList.Add(new LevelEnemyEntry { enemy = new EnemyData { name = kv.Key.ToString(), cost = kv.Value }, startingBudget = s0, endingBudget = s1 });
        }
        if (stealthShare.HasValue)
        {
            // stealth gets share p of the total designer budget at every moment; the others keep their designer ratios
            var st = lv.enemyDataList.First(e => e.startingBudget < 0f);
            double p = stealthShare.Value;
            float o0 = lv.enemyDataList.Where(e => e != st).Sum(e => e.startingBudget), o1 = lv.enemyDataList.Where(e => e != st).Sum(e => e.endingBudget);
            if (p >= 1.0) { st.startingBudget = st.endingBudget = 1f; }
            else { st.startingBudget = (float)(p / (1 - p) * o0); st.endingBudget = (float)(p / (1 - p) * o1); }
        }
        return lv;
    }

    public static CombatResult Run(Variant v, int seed, string loadout, Fam? only = null)
    {
        return Run(v, seed, loadout, new CombatOptions { Only = only });
    }

    /// <summary>One level of combat. only = send just this family (used for the hindsight best-family reference).</summary>
    public static CombatResult Run(Variant v, int seed, string loadout, CombatOptions opt)
    {
        opt = opt ?? new CombatOptions();
        var rng = new Random(seed * 104729 + 7);
        var lv = MakeLevel(v, opt.Only, opt.StealthShare);
        lv.Init();
        var b = new WaveDirectorBridge { level = lv, seed = seed };
        b.useRules = v == Variant.RulesOnly || v == Variant.RulesPlusLearning;
        b.useLearning = v == Variant.LearningOnly || v == Variant.RulesPlusLearning;
        b.BeginLevel();
        if (opt.Configure != null) opt.Configure(b);
        float range = CombatAssumptions.TowerRange * opt.RangeMul;

        var towers = new List<Tower>();
        var lo = Loadouts[loadout];
        for (int i = 0; i < lo.Length; i++)
        {
            if (!lo[i].HasValue) continue;
            float hp, d, f; Readme.Tower(lo[i].Value, out hp, out d, out f);
            towers.Add(new Tower { Kind = lo[i].Value, Slot = i, Hp = hp });
        }
        var foes = new List<Foe>();
        var res = new CombatResult();
        float fuel = CombatAssumptions.StartingFuel;
        float slotDeg = 360f / CombatAssumptions.Slots;
        float T = lv.maxTime;

        for (float t = 0; t <= T; t += CombatAssumptions.Dt)
        {
            float rot = -CombatAssumptions.RotationDegPerSec * t; // clockwise
            Func<Tower, float> towerAngle = tw => Norm(tw.Slot * slotDeg + rot);

            // --- AI snapshot (once per second is plenty)
            var snap = new DefenseSnapshot();
            float fuelRate = 0, passive = 0;
            foreach (var tw in towers)
            {
                if (!tw.Alive) continue;
                float hp, d, f; Readme.Tower(tw.Kind, out hp, out d, out f);
                if (!opt.NewFuel) fuelRate += f;
                else
                {
                    // game since 2026-11-08: generators are passive; weapons pay |intake| per shot; the AI sees the estimate at full fire rate
                    if (f >= 0) passive += f;
                    fuelRate += f >= 0 ? f : (TowerCatalog.Attacks(tw.Kind) ? f * CombatAssumptions.FireRate(tw.Kind) * opt.FireRateMul : f);
                }
                snap.Towers.Add(new TowerInfo { Kind = tw.Kind, AngleDeg = towerAngle(tw), Hp = tw.Hp, IsAlive = true });
            }
            snap.FuelPerSecond = fuelRate;
            if (!opt.NewFuel) fuel = Math.Max(0, fuel + fuelRate * CombatAssumptions.Dt);
            else fuel += passive * CombatAssumptions.Dt;
            bool powered = opt.NewFuel || fuel > 0;

            // --- spawn (one pick per 0.5s, like the real spawner)
            if (Math.Abs(t * 2 - Math.Round(t * 2)) < 1e-3)
            {
                b.Tick(t, snap);
                var e = b.PickEntry(t);
                if (e != null)
                {
                    var kind = b.KindOf(e.enemy);
                    var mods = b.RollModifiers(e, snap);
                    float hp, d; bool a; Readme.Enemy(kind, out hp, out d, out a);
                    if (mods.IsShiny) hp *= 2;
                    foes.Add(new Foe { Kind = kind, Ticket = mods.Ticket, Angle = mods.SpawnAngleDeg, R = CombatAssumptions.SpawnRadius, Hp = hp, MaxHp = hp,
                        Shield = EnemyCatalog.IsShielded(kind), Orbit = mods.Orbits || EnemyCatalog.IsDeer(kind), Stealth = kind == EnemyKind.StealthGoose, Shiny = mods.IsShiny,
                        Cooldown = (float)rng.NextDouble() * CombatAssumptions.AttackCooldown(kind) });
                    if (t >= T * 0.2f) { res.Spent += e.enemy.cost; res.FamCost[(int)Bench.FamOfPublic(kind)] += e.enemy.cost; }
                }
            }

            // --- enemies move and attack
            foreach (var f in foes)
            {
                float speedMul = f.Slow > 0 ? 0.5f : 1f;
                if (f.Slow > 0) f.Slow -= CombatAssumptions.Dt;
                float target = f.Orbit ? CombatAssumptions.OrbitRadius : CombatAssumptions.StopRadius;
                if (f.R > target) { f.R = Math.Max(target, f.R - CombatAssumptions.ApproachSpeed * speedMul * CombatAssumptions.Dt); }
                else f.Arrived = true;
                if (f.Orbit && f.Arrived) f.Angle = Norm(f.Angle + CombatAssumptions.OrbitDegPerSec * speedMul * CombatAssumptions.Dt);

                if (f.DotLeft > 0) { Hurt(f, f.Dot * CombatAssumptions.Dt, ignoreShield: true); f.DotLeft -= CombatAssumptions.Dt; }

                if (!f.Arrived || f.Hp <= 0) continue;
                f.Cooldown -= CombatAssumptions.Dt;
                if (f.Cooldown > 0) continue;
                f.Cooldown = CombatAssumptions.AttackCooldown(f.Kind);

                float hp, dmg; bool aoe; Readme.Enemy(f.Kind, out hp, out dmg, out aoe);
                int below = (int)Math.Floor(Norm(f.Angle - rot + slotDeg / 2) / slotDeg) % CombatAssumptions.Slots;
                var hitSlots = aoe ? new[] { below, (below + 1) % CombatAssumptions.Slots, (below + CombatAssumptions.Slots - 1) % CombatAssumptions.Slots } : new[] { below };
                float dealt = 0;
                foreach (int s in hitSlots)
                {
                    var tw = towers.FirstOrDefault(x => x.Alive && x.Slot == s);
                    bool center = s == below;
                    if (tw != null) { float d = Math.Min(dmg, tw.Hp); tw.Hp -= dmg; dealt += d; if (tw.Hp <= 0) { tw.Alive = false; if (t >= T * 0.2f) res.TowersLost++; } }
                    else if (center) { dealt += dmg; if (t >= T * 0.2f) res.PlanetDamage += dmg; } // splash on empty side slots hits nothing
                }
                b.ReportDamage(f.Ticket, dealt);
                if (t >= T * 0.2f) res.DamageToPlayer += dealt;
            }

            // --- towers attack
            if (powered)
            {
                var revealAngles = towers.Where(x => x.Alive && TowerCatalog.RevealsStealth(x.Kind)).Select(x => Tuple.Create(towerAngle(x), TowerCatalog.Fov(x.Kind) / 2 * opt.RevealMul)).ToList();
                foreach (var tw in towers)
                {
                    if (!tw.Alive || !TowerCatalog.Attacks(tw.Kind)) continue;
                    tw.Cooldown -= CombatAssumptions.Dt;
                    if (tw.Cooldown > 0) continue;
                    float ta = towerAngle(tw), half = TowerCatalog.Fov(tw.Kind) / 2;
                    var inView = foes.Where(f => f.Hp > 0 && f.R <= range && AdaptiveWaveDirector.AngleDistance(f.Angle, ta) <= half
                        && (!f.Stealth || revealAngles.Any(r => AdaptiveWaveDirector.AngleDistance(f.Angle, r.Item1) <= r.Item2))).ToList();
                    if (inView.Count == 0) continue;
                    float thp, tdmg, tf; Readme.Tower(tw.Kind, out thp, out tdmg, out tf);
                    if (opt.NewFuel && tf < 0)
                    {
                        if (fuel < -tf) continue;   // can't afford this shot
                        fuel += tf;
                    }
                    tw.Cooldown = 1f / (CombatAssumptions.FireRate(tw.Kind) * opt.FireRateMul);

                    if (tw.Kind == TowerKind.GammaRadar)
                    {
                        foreach (var f in inView) Hurt(f, tdmg);
                        continue;
                    }
                    var tgt = inView.OrderBy(f => f.R).ThenBy(f => AdaptiveWaveDirector.AngleDistance(f.Angle, ta)).First();
                    if (tw.Kind == TowerKind.PestTrap)
                    {
                        var fresh = inView.Where(f => f.DotLeft <= 0).OrderBy(f => f.R).FirstOrDefault() ?? tgt;
                        fresh.Dot = 1f; fresh.DotLeft = 3f; fresh.Slow = 3f;
                    }
                    else if (TowerCatalog.IsAoe(tw.Kind))
                    {
                        foreach (var f in inView.Where(f => AdaptiveWaveDirector.AngleDistance(f.Angle, tgt.Angle) <= CombatAssumptions.AoeRadiusDeg)) Hurt(f, tdmg);
                    }
                    else if (tw.Kind == TowerKind.Ballistica) Hurt(tgt, Math.Max(1f, 0.2f * tgt.MaxHp));
                    else Hurt(tgt, tdmg);
                }
            }

            foreach (var f in foes.Where(f => f.Hp <= 0)) { b.ReportGone(f.Ticket); fuel += Readme.FuelDrop(f.Kind) * (f.Shiny ? 2 : 1); res.Kills++; }
            foes.RemoveAll(f => f.Hp <= 0);
        }
        foreach (var f in foes) b.ReportGone(f.Ticket);
        if (opt.Posterior && b.Director != null)
        {
            var reps = new[] { EnemyKind.BasicGoose1, EnemyKind.StealthGoose, EnemyKind.BasicSkunk1, EnemyKind.BasicDeer };
            var L = b.Director.Learner;
            res.PosteriorMean = reps.Select(k => (double)L.MeanReward(k)).ToArray();
            res.FactorAtCap = new double[4]; res.DrawMean = new double[4];
            for (int i = 0; i < 4; i++)
            {
                int hit = 0; double sum = 0; const int D = 400;
                for (int d = 0; d < D; d++) { float fct = L.SampleFactor(reps[i]); sum += fct; if (fct >= L.Settings.MaxFactor * 0.95f) hit++; }
                res.FactorAtCap[i] = hit / (double)D; res.DrawMean[i] = sum / D;
            }
        }
        return res;
    }

    static void Hurt(Foe f, float amount, bool ignoreShield = false)
    {
        if (f.Shield && !ignoreShield) { f.Shield = false; return; }
        f.Hp -= amount;
    }
}
