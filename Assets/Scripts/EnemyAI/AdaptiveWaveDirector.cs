// Enemy AI (Wave Director) - adaptive layer.
//
// The team's LevelData already decides HOW MUCH to spawn (budget curve) and the
// base spawn probability of each enemy type. This class sits on top of that:
//   1. Reads the player's defenses and multiplies each enemy's probability by an
//      adaptive factor (capped, delayed at the start of a level).
//   2. Picks the enemy to spawn.
//   3. Picks the spawn angle (60% weakest direction, 40% random).
//   4. Rolls shiny / orbiting.
//   5. Produces a one-line "intent" for the HUD.
//   6. Learns online (EnemyLearner, Thompson sampling) which enemy types actually
//      hurt this player, and shifts picks toward them on top of the rules.
//
// Pure C#: no UnityEngine dependency.

using System;
using System.Collections.Generic;

namespace EnemyAI
{
    public class WaveDirectorSettings
    {
        public float MaxMultiplier = 1.5f;            // fairness cap per enemy
        public float AdaptationStartProgress = 0.2f;  // no adaptation before 20% of the level
        public float ReevaluateIntervalSec = 15f;     // how often multipliers are recomputed
        public float WeakSpotSpawnChance = 0.6f;      // 60% aim at the weakest direction
        public int AngleSamples = 36;                 // candidate angles for weak-spot search
        public float ShinyChance = 0.05f;
        public float BossShinyChance = 0.01f;
        public float OrbitChance = 0.5f;              // geese and skunks; deer always orbit
        public float BossWarningLeadSec = 5f;
        public float BossAliveBudgetScale = 0.5f;     // normal budget while the boss is alive
        public float ClusterNeighborDeg = 30f;        // towers closer than this count as clustered

        // Thresholds for the rules
        public float HeavyShareThreshold = 0.4f;
        public float AoeShareThreshold = 0.4f;
        public float ClusterShareThreshold = 0.5f;
    }

    public class AdaptiveWaveDirector
    {
        public WaveDirectorSettings Settings { get; private set; }

        /// <summary>Short line for the HUD, e.g. "The flock noticed you have no radar." Empty when not adapting.</summary>
        public string CurrentIntent { get; private set; }

        /// <summary>The online learning layer. Report spawns, damage and deaths to it (via RollModifiers / ReportDamage / ReportGone).</summary>
        public EnemyLearner Learner { get; private set; }

        /// <summary>Set false to turn the learning layer off (rules only).</summary>
        public bool UseLearning = true;

        private readonly Random rng;
        private readonly Dictionary<EnemyKind, float> multipliers = new Dictionary<EnemyKind, float>();
        private float lastEvaluationTime = float.NegativeInfinity;
        private bool adaptationActive;

        public AdaptiveWaveDirector(int? seed = null, WaveDirectorSettings settings = null, EnemyLearnerSettings learnerSettings = null)
        {
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
            Settings = settings ?? new WaveDirectorSettings();
            Learner = new EnemyLearner(rng, learnerSettings);
            CurrentIntent = "";
        }

        /// <summary>
        /// Call when a level starts. By default the learner keeps what it learned about
        /// this player across levels; pass true to forget it.
        /// </summary>
        public void Reset(bool forgetLearning = false)
        {
            multipliers.Clear();
            lastEvaluationTime = float.NegativeInfinity;
            adaptationActive = false;
            CurrentIntent = "";
            Learner.ClearOpenTickets();
            if (forgetLearning) Learner.ResetAll();
        }

        /// <summary>Call whenever an enemy deals damage. ticket = SpawnModifiers.Ticket.</summary>
        public void ReportDamage(int ticket, float amount)
        {
            Learner.ReportDamage(ticket, amount);
        }

        /// <summary>Call once when an enemy dies or despawns. ticket = SpawnModifiers.Ticket.</summary>
        public void ReportGone(int ticket)
        {
            Learner.ReportGone(ticket);
        }

        // ------------------------------------------------------------------
        // 1. Adaptive multipliers
        // ------------------------------------------------------------------

        /// <summary>
        /// Recomputes multipliers if the re-evaluation interval has passed.
        /// Call every frame or every spawn tick; it only does work every 15s.
        /// </summary>
        public void Update(float elapsedTime, float levelProgress, DefenseSnapshot snapshot)
        {
            if (levelProgress < Settings.AdaptationStartProgress)
            {
                // Still collecting results for the learner, but not acting on them yet.
                multipliers.Clear();
                adaptationActive = false;
                CurrentIntent = "";
                return;
            }

            adaptationActive = true;

            if (elapsedTime - lastEvaluationTime < Settings.ReevaluateIntervalSec)
            {
                return;
            }

            lastEvaluationTime = elapsedTime;
            Evaluate(snapshot);

            if (UseLearning)
            {
                Learner.Decay();

                // A confident learned insight beats a rule-based one on the HUD.
                EnemyKind best;
                float bestMean;
                if (Learner.TryGetBestLearned(out best, out bestMean))
                {
                    CurrentIntent = "They've learned " + EnemyLearner.DisplayName(best) + " hurt you the most.";
                }
            }
        }

        /// <summary>Current multiplier for an enemy type (1 = no change).</summary>
        public float GetMultiplier(EnemyKind kind)
        {
            float m;
            return multipliers.TryGetValue(kind, out m) ? m : 1f;
        }

        private void Evaluate(DefenseSnapshot snapshot)
        {
            multipliers.Clear();
            CurrentIntent = "";

            var raw = new Dictionary<EnemyKind, float>();
            float strongest = 0f;
            string strongestIntent = "";

            Action<IEnumerable<EnemyKind>, float, string> apply = (kinds, factor, intent) =>
            {
                foreach (EnemyKind k in kinds)
                {
                    float current;
                    raw[k] = (raw.TryGetValue(k, out current) ? current : 1f) * factor;
                }
                if (factor > strongest)
                {
                    strongest = factor;
                    strongestIntent = intent;
                }
            };

            List<TowerInfo> alive = new List<TowerInfo>();
            if (snapshot != null && snapshot.Towers != null)
            {
                foreach (TowerInfo t in snapshot.Towers)
                {
                    if (t != null && t.IsAlive) alive.Add(t);
                }
            }

            int attackers = 0, heavy = 0, aoe = 0;
            bool canSeeStealth = false;
            foreach (TowerInfo t in alive)
            {
                if (TowerCatalog.RevealsStealth(t.Kind)) canSeeStealth = true;
                if (!TowerCatalog.Attacks(t.Kind)) continue;
                attackers++;
                if (TowerCatalog.IsHeavySingle(t.Kind)) heavy++;
                if (TowerCatalog.IsAoe(t.Kind)) aoe++;
            }

            // Rule 1: no Radar / Gamma Radar -> stealth geese
            if (!canSeeStealth)
            {
                apply(new[] { EnemyKind.StealthGoose }, 1.5f,
                    "The flock noticed you have no radar.");
            }

            // Rule 2: heavy single hits -> shields and swarms
            if (attackers > 0 && (float)heavy / attackers >= Settings.HeavyShareThreshold)
            {
                apply(new[] { EnemyKind.ShieldedGoose, EnemyKind.ShieldedSkunk, EnemyKind.ShieldedDeer }, 1.4f,
                    "Shields up: they're ready for your snipers.");
                apply(new[] { EnemyKind.BasicGoose1, EnemyKind.BasicGoose2, EnemyKind.BasicGoose3 }, 1.3f,
                    "Shields up: they're ready for your snipers.");
            }

            // Rule 3: AoE-heavy -> few high-HP deer
            if (attackers > 0 && (float)aoe / attackers >= Settings.AoeShareThreshold)
            {
                apply(new[] { EnemyKind.BasicDeer, EnemyKind.ShieldedDeer }, 1.5f,
                    "The herd is coming. Your bombs won't scare them.");
            }

            // Rule 4: towers clustered together -> skunks (AoE)
            if (ClusterShare(alive) >= Settings.ClusterShareThreshold)
            {
                apply(new[] { EnemyKind.BasicSkunk1, EnemyKind.BasicSkunk2, EnemyKind.BasicSkunk3, EnemyKind.ShieldedSkunk }, 1.4f,
                    "Skunks smell a crowd. Spread out your towers.");
            }

            // Rule 5: fuel deficit -> long fights with high-HP enemies
            if (snapshot != null && snapshot.FuelPerSecond < 0f)
            {
                apply(new[] { EnemyKind.BasicSkunk3, EnemyKind.ShieldedSkunk, EnemyKind.BasicDeer, EnemyKind.ShieldedDeer }, 1.3f,
                    "They're waiting for your fuel to run out.");
            }

            foreach (KeyValuePair<EnemyKind, float> kv in raw)
            {
                multipliers[kv.Key] = Math.Min(kv.Value, Settings.MaxMultiplier);
            }

            CurrentIntent = strongestIntent;
        }

        private float ClusterShare(List<TowerInfo> alive)
        {
            if (alive.Count < 2) return 0f;
            int clustered = 0;
            for (int i = 0; i < alive.Count; i++)
            {
                for (int j = 0; j < alive.Count; j++)
                {
                    if (i == j) continue;
                    if (AngleDistance(alive[i].AngleDeg, alive[j].AngleDeg) <= Settings.ClusterNeighborDeg)
                    {
                        clustered++;
                        break;
                    }
                }
            }
            return (float)clustered / alive.Count;
        }

        // ------------------------------------------------------------------
        // 2. Picking the enemy
        // ------------------------------------------------------------------

        public class Candidate
        {
            public EnemyKind Kind;
            public float BaseProbability; // from LevelData (SpawnProbability)
            public int Cost;
            public object Tag;            // anything the caller wants back (e.g. LevelEnemyEntry)
        }

        /// <summary>
        /// Multiplies each base probability by its adaptive factor, renormalizes,
        /// skips enemies heavier than the remaining budget, and picks one.
        /// Returns null if nothing fits.
        /// </summary>
        public Candidate PickEnemy(IList<Candidate> candidates, float remainingBudget)
        {
            if (candidates == null || candidates.Count == 0) return null;

            float total = 0f;
            var weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate c = candidates[i];
                if (c == null || c.BaseProbability <= 0f || c.Cost > remainingBudget) continue;
                float w = c.BaseProbability * GetMultiplier(c.Kind);
                if (UseLearning && adaptationActive) w *= Learner.SampleFactor(c.Kind); // Thompson sample
                weights[i] = w;
                total += w;
            }

            if (total <= 0f) return null;

            double roll = rng.NextDouble() * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (weights[i] <= 0f) continue;
                roll -= weights[i];
                if (roll <= 0) return candidates[i];
            }

            // Floating point fallback: last valid candidate
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (weights[i] > 0f) return candidates[i];
            }
            return null;
        }

        // ------------------------------------------------------------------
        // 3. Spawn angle
        // ------------------------------------------------------------------

        /// <summary>
        /// 60%: the least-covered direction (FOV blind spots, empty areas).
        /// 40%: fully random.
        /// Towers are assumed to face straight outward from the planet.
        /// </summary>
        public float ChooseSpawnAngle(DefenseSnapshot snapshot)
        {
            if (rng.NextDouble() >= Settings.WeakSpotSpawnChance)
            {
                return (float)(rng.NextDouble() * 360.0);
            }

            int samples = Math.Max(4, Settings.AngleSamples);
            float step = 360f / samples;
            float offset = (float)(rng.NextDouble() * step); // avoid always using the same grid

            int bestCoverage = int.MaxValue;
            var bestAngles = new List<float>();

            for (int i = 0; i < samples; i++)
            {
                float angle = (offset + i * step) % 360f;
                int coverage = Coverage(snapshot, angle);
                if (coverage < bestCoverage)
                {
                    bestCoverage = coverage;
                    bestAngles.Clear();
                    bestAngles.Add(angle);
                }
                else if (coverage == bestCoverage)
                {
                    bestAngles.Add(angle);
                }
            }

            return bestAngles[rng.Next(bestAngles.Count)];
        }

        /// <summary>Number of alive attacking towers whose FOV covers this angle.</summary>
        public int Coverage(DefenseSnapshot snapshot, float angleDeg)
        {
            if (snapshot == null || snapshot.Towers == null) return 0;
            int count = 0;
            foreach (TowerInfo t in snapshot.Towers)
            {
                if (t == null || !t.IsAlive || !TowerCatalog.Attacks(t.Kind)) continue;
                if (AngleDistance(t.AngleDeg, angleDeg) <= TowerCatalog.Fov(t.Kind) * 0.5f) count++;
            }
            return count;
        }

        // ------------------------------------------------------------------
        // 4. Shiny / orbit
        // ------------------------------------------------------------------

        /// <summary>Rolls shiny / orbit / angle and registers the spawn with the learner (see SpawnModifiers.Ticket).</summary>
        public SpawnModifiers RollModifiers(EnemyKind kind, DefenseSnapshot snapshot)
        {
            int cost;
            if (!EnemyCatalog.DefaultWeight.TryGetValue(kind, out cost)) cost = 10;
            return RollModifiers(kind, cost, snapshot);
        }

        public SpawnModifiers RollModifiers(EnemyKind kind, int cost, DefenseSnapshot snapshot)
        {
            bool boss = EnemyCatalog.IsBoss(kind);
            var mods = new SpawnModifiers();
            mods.Ticket = boss ? 0 : Learner.Register(kind, cost); // bosses are not learned
            mods.IsShiny = rng.NextDouble() < (boss ? Settings.BossShinyChance : Settings.ShinyChance);

            if (EnemyCatalog.IsDeer(kind)) mods.Orbits = true;
            else if (EnemyCatalog.IsGoose(kind) || EnemyCatalog.IsSkunk(kind)) mods.Orbits = rng.NextDouble() < Settings.OrbitChance;
            else mods.Orbits = false; // bosses: decided by their own behavior

            mods.SpawnAngleDeg = ChooseSpawnAngle(snapshot);
            return mods;
        }

        // ------------------------------------------------------------------
        // 5. Boss helpers
        // ------------------------------------------------------------------

        /// <summary>True exactly once, when the timer crosses (bossTime - warning lead).</summary>
        public bool ShouldShowBossWarning(float previousTime, float time, float bossTime)
        {
            float warnAt = bossTime - Settings.BossWarningLeadSec;
            return previousTime < warnAt && time >= warnAt;
        }

        /// <summary>Scale for the normal-enemy budget (0.5 while a boss is alive).</summary>
        public float BudgetScale(bool bossAlive)
        {
            return bossAlive ? Settings.BossAliveBudgetScale : 1f;
        }

        // ------------------------------------------------------------------

        public static float AngleDistance(float a, float b)
        {
            float d = Math.Abs(((a - b) % 360f + 360f) % 360f);
            return d > 180f ? 360f - d : d;
        }
    }
}
