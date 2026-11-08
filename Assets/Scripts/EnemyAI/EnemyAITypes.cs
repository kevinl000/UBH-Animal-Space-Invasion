// Enemy AI (Wave Director) - shared types.
// Pure C#: no UnityEngine dependency, so it can be unit-tested outside Unity.

using System.Collections.Generic;

namespace EnemyAI
{
    public enum EnemyKind
    {
        BasicGoose1,
        BasicGoose2,
        BasicGoose3,
        ShieldedGoose,
        StealthGoose,
        BasicSkunk1,
        BasicSkunk2,
        BasicSkunk3,
        ShieldedSkunk,
        BasicDeer,
        ShieldedDeer,

        // Bosses
        HealthyGoose,
        HealthySkunk,
        MotherGoose,
        MotherSkunk,
        RestrainingBuck,
        SneakyHealthyGoose,
        AnimalShip,
        EndangeredDeer,
    }

    public enum TowerKind
    {
        Laser,
        Bomb,
        Sniper,
        PestTrap,
        Radar,
        OilRig,
        Bank,
        LaserSMG,
        TeslaCoil,
        GammaRadar,
        Ballistica,
    }

    /// <summary>One tower as the AI sees it at this moment.</summary>
    public class TowerInfo
    {
        public TowerKind Kind;
        /// <summary>World angle of the tower on its planet, in degrees (0-360).</summary>
        public float AngleDeg;
        public float Hp;
        public bool IsAlive = true;
    }

    /// <summary>What the AI reads from the game every time it re-evaluates.</summary>
    public class DefenseSnapshot
    {
        public List<TowerInfo> Towers = new List<TowerInfo>();
        /// <summary>Estimated net fuel/second at full activity (generation minus shot cost / attack interval).</summary>
        public float FuelPerSecond;
        public float Fuel;
        public float Money;
        public float PlanetHp;
    }

    /// <summary>Per-spawn random modifiers rolled by the director.</summary>
    public struct SpawnModifiers
    {
        public bool IsShiny;    // double HP, double drops
        public bool Orbits;     // orbit the planet instead of stopping
        public float SpawnAngleDeg;
        /// <summary>Keep this on the spawned enemy and pass it to ReportDamage / ReportGone. 0 = not tracked (bosses).</summary>
        public int Ticket;
    }

    public static class EnemyCatalog
    {
        public static bool IsBoss(EnemyKind k)
        {
            return k >= EnemyKind.HealthyGoose;
        }

        public static bool IsGoose(EnemyKind k)
        {
            return k == EnemyKind.BasicGoose1 || k == EnemyKind.BasicGoose2 || k == EnemyKind.BasicGoose3
                || k == EnemyKind.ShieldedGoose || k == EnemyKind.StealthGoose;
        }

        public static bool IsSkunk(EnemyKind k)
        {
            return k == EnemyKind.BasicSkunk1 || k == EnemyKind.BasicSkunk2 || k == EnemyKind.BasicSkunk3
                || k == EnemyKind.ShieldedSkunk;
        }

        public static bool IsDeer(EnemyKind k)
        {
            return k == EnemyKind.BasicDeer || k == EnemyKind.ShieldedDeer;
        }

        public static bool IsShielded(EnemyKind k)
        {
            return k == EnemyKind.ShieldedGoose || k == EnemyKind.ShieldedSkunk || k == EnemyKind.ShieldedDeer;
        }

        /// <summary>
        /// Suggested weight (cost) per enemy: HP + 2 x Damage, +2 shielded, +2 stealth.
        /// Copy these into EnemyData.cost, or tune them there.
        /// </summary>
        public static readonly Dictionary<EnemyKind, int> DefaultWeight = new Dictionary<EnemyKind, int>
        {
            { EnemyKind.BasicGoose1, 3 },
            { EnemyKind.BasicGoose2, 4 },
            { EnemyKind.BasicGoose3, 6 },
            { EnemyKind.ShieldedGoose, 8 },
            { EnemyKind.StealthGoose, 5 },
            { EnemyKind.BasicSkunk1, 6 },
            { EnemyKind.BasicSkunk2, 8 },
            { EnemyKind.BasicSkunk3, 12 },
            { EnemyKind.ShieldedSkunk, 14 },
            { EnemyKind.BasicDeer, 20 },
            { EnemyKind.ShieldedDeer, 22 },
        };
    }

    public static class TowerCatalog
    {
        /// <summary>FOV in degrees (README). 0 = does not attack.</summary>
        public static float Fov(TowerKind k)
        {
            switch (k)
            {
                case TowerKind.Radar: return 90f;
                case TowerKind.OilRig:
                case TowerKind.Bank: return 0f;
                default: return 120f;
            }
        }

        public static bool Attacks(TowerKind k)
        {
            return k != TowerKind.Radar && k != TowerKind.OilRig && k != TowerKind.Bank;
        }

        public static bool RevealsStealth(TowerKind k)
        {
            return k == TowerKind.Radar || k == TowerKind.GammaRadar;
        }

        /// <summary>Big single hits that a shield wastes.</summary>
        public static bool IsHeavySingle(TowerKind k)
        {
            return k == TowerKind.Sniper || k == TowerKind.Ballistica;
        }

        public static bool IsAoe(TowerKind k)
        {
            return k == TowerKind.Bomb || k == TowerKind.TeslaCoil || k == TowerKind.GammaRadar;
        }
    }
}
