// Enemy AI - online learning layer (Thompson sampling, multi-armed bandit).
//
// Each enemy type is an "arm". Every time an enemy of that type leaves play
// (killed or despawned), we score how well it did:
//     reward = damage / (damage + cost)        -> 0 if it did no damage, close to 1 if it did a lot for its cost
// and update a Beta(alpha, beta) belief for that type.
//
// When picking the next enemy, we SAMPLE a success rate from each type's belief.
// Types that hurt this player get picked more; types we know little about still
// get tried sometimes (exploration). Old results slowly fade (decay), so the AI
// re-adapts when the player changes their defense.
//
// Pure C#: no UnityEngine dependency.

using System;
using System.Collections.Generic;

namespace EnemyAI
{
    public class EnemyLearnerSettings
    {
        public float PriorAlpha = 2f;          // Beta(2,2): mean 0.5, "we don't know yet"
        public float PriorBeta = 2f;
        public float MinFactor = 0.5f;         // learned factor is clamped to [0.5, 2]
        public float MaxFactor = 2f;
        public float DecayPerStep = 0.9f;      // how much evidence is kept each Decay() call
        public int MinSamplesForIntent = 4;    // results needed before the HUD claims it "learned"
        public float IntentLeadMargin = 0.12f; // best type must beat the average by this much
    }

    public class EnemyLearner
    {
        private class Arm
        {
            public float Alpha;
            public float Beta;
            public int Samples;
        }

        private class Ticket
        {
            public EnemyKind Kind;
            public int Cost;
            public float Damage;
        }

        public EnemyLearnerSettings Settings { get; private set; }

        private readonly Random rng;
        private readonly Dictionary<EnemyKind, Arm> arms = new Dictionary<EnemyKind, Arm>();
        private readonly Dictionary<int, Ticket> open = new Dictionary<int, Ticket>();
        private int nextTicket = 1;

        public EnemyLearner(Random rng, EnemyLearnerSettings settings = null)
        {
            this.rng = rng;
            Settings = settings ?? new EnemyLearnerSettings();
        }

        /// <summary>Forget everything (e.g. new player / new run).</summary>
        public void ResetAll()
        {
            arms.Clear();
            open.Clear();
        }

        /// <summary>Drop tickets of enemies that are still alive (call at level end).</summary>
        public void ClearOpenTickets()
        {
            open.Clear();
        }

        // ------------------------------------------------------------------
        // Reporting from the game
        // ------------------------------------------------------------------

        /// <summary>Call when an enemy spawns. Keep the returned ticket on the enemy.</summary>
        public int Register(EnemyKind kind, int cost)
        {
            int id = nextTicket++;
            var t = new Ticket();
            t.Kind = kind;
            t.Cost = Math.Max(1, cost);
            open[id] = t;
            return id;
        }

        /// <summary>Call whenever that enemy deals damage (to a tower or the planet).</summary>
        public void ReportDamage(int ticket, float amount)
        {
            Ticket t;
            if (amount > 0f && open.TryGetValue(ticket, out t)) t.Damage += amount;
        }

        /// <summary>Call once when that enemy dies or despawns. This is when learning happens.</summary>
        public void ReportGone(int ticket)
        {
            Ticket t;
            if (!open.TryGetValue(ticket, out t)) return;
            open.Remove(ticket);

            float reward = t.Damage / (t.Damage + t.Cost);
            Arm a = GetArm(t.Kind);
            a.Alpha += reward;
            a.Beta += 1f - reward;
            a.Samples++;
        }

        /// <summary>Fade old evidence toward the prior so the AI keeps adapting. Call periodically.</summary>
        public void Decay()
        {
            float k = Settings.DecayPerStep;
            foreach (Arm a in arms.Values)
            {
                a.Alpha = Settings.PriorAlpha + (a.Alpha - Settings.PriorAlpha) * k;
                a.Beta = Settings.PriorBeta + (a.Beta - Settings.PriorBeta) * k;
            }
        }

        // ------------------------------------------------------------------
        // Used by the director
        // ------------------------------------------------------------------

        /// <summary>
        /// Thompson sample: draw a success rate from this type's belief and turn it
        /// into a probability multiplier (prior mean -> 1x), clamped to [MinFactor, MaxFactor].
        /// </summary>
        public float SampleFactor(EnemyKind kind)
        {
            Arm a = GetArm(kind);
            double theta = SampleBeta(a.Alpha, a.Beta);
            double priorMean = Settings.PriorAlpha / (Settings.PriorAlpha + Settings.PriorBeta);
            double f = theta / priorMean;
            return (float)Math.Max(Settings.MinFactor, Math.Min(Settings.MaxFactor, f));
        }

        /// <summary>Current estimated success rate (for debugging / HUD).</summary>
        public float MeanReward(EnemyKind kind)
        {
            Arm a = GetArm(kind);
            return a.Alpha / (a.Alpha + a.Beta);
        }

        public int SampleCount(EnemyKind kind)
        {
            Arm a;
            return arms.TryGetValue(kind, out a) ? a.Samples : 0;
        }

        /// <summary>
        /// The type the AI has learned works best against this player, if it is confident.
        /// Returns false when there is not enough evidence yet.
        /// </summary>
        public bool TryGetBestLearned(out EnemyKind best, out float bestMean)
        {
            best = EnemyKind.BasicGoose1;
            bestMean = 0f;
            float sum = 0f;
            int count = 0;

            foreach (KeyValuePair<EnemyKind, Arm> kv in arms)
            {
                if (kv.Value.Samples < Settings.MinSamplesForIntent) continue;
                float m = kv.Value.Alpha / (kv.Value.Alpha + kv.Value.Beta);
                sum += m;
                count++;
                if (m > bestMean)
                {
                    bestMean = m;
                    best = kv.Key;
                }
            }

            if (count < 2) return false;
            float avg = sum / count;
            return bestMean - avg >= Settings.IntentLeadMargin;
        }

        // ------------------------------------------------------------------

        private Arm GetArm(EnemyKind kind)
        {
            Arm a;
            if (!arms.TryGetValue(kind, out a))
            {
                a = new Arm();
                a.Alpha = Settings.PriorAlpha;
                a.Beta = Settings.PriorBeta;
                arms[kind] = a;
            }
            return a;
        }

        // Beta(a, b) = X / (X + Y), X ~ Gamma(a), Y ~ Gamma(b)
        private double SampleBeta(double a, double b)
        {
            double x = SampleGamma(a);
            double y = SampleGamma(b);
            return x / (x + y);
        }

        // Marsaglia-Tsang gamma sampler
        private double SampleGamma(double shape)
        {
            if (shape < 1.0)
            {
                double u = rng.NextDouble();
                return SampleGamma(shape + 1.0) * Math.Pow(u, 1.0 / shape);
            }

            double d = shape - 1.0 / 3.0;
            double c = 1.0 / Math.Sqrt(9.0 * d);
            while (true)
            {
                double x, v;
                do
                {
                    x = SampleNormal();
                    v = 1.0 + c * x;
                } while (v <= 0.0);

                v = v * v * v;
                double u = rng.NextDouble();
                if (u < 1.0 - 0.0331 * x * x * x * x) return d * v;
                if (Math.Log(u) < 0.5 * x * x + d * (1.0 - v + Math.Log(v))) return d * v;
            }
        }

        private double SampleNormal()
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        public static string DisplayName(EnemyKind kind)
        {
            if (EnemyCatalog.IsGoose(kind)) return kind == EnemyKind.StealthGoose ? "stealth geese" : (EnemyCatalog.IsShielded(kind) ? "shielded geese" : "geese");
            if (EnemyCatalog.IsSkunk(kind)) return EnemyCatalog.IsShielded(kind) ? "shielded skunks" : "skunks";
            if (EnemyCatalog.IsDeer(kind)) return EnemyCatalog.IsShielded(kind) ? "shielded deer" : "deer";
            return kind.ToString();
        }
    }
}
