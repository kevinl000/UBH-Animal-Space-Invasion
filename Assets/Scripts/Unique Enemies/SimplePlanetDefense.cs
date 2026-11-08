using System.Collections.Generic;
using UnityEngine;
using EnemyAI;

/// <summary>
/// A simple, working IPlanetDefense: a rotating planet with N slots and its own HP.
/// Tower scripts call RegisterTower / UnregisterTower; the economy calls SetFuelPerSecond.
/// Replace or extend it when the real planet/tower code exists.
/// </summary>
public class SimplePlanetDefense : MonoBehaviour, IPlanetDefense
{
    [Tooltip("Enemies are parented here. Defaults to this transform.")]
    public Transform root;
    [Tooltip("Planet radius in root local units (UI pixels on a canvas).")]
    public float radius = 200f;
    [Tooltip("Tower slots around the planet (README: small 6, small-medium 8, medium 12, big 16).")]
    public int slots = 12;
    [Tooltip("Degrees per second. README planets rotate 'normal' speed.")]
    public float rotationDegreesPerSecond = 6f;
    public bool clockwise = true;
    [Tooltip("Angle of slot 0 before any rotation.")]
    public float slot0Angle = 90f;
    [Tooltip("Planet HP. If a LevelData is assigned, its planetMaxHealth is used instead.")]
    public float planetMaxHealth = 100f;
    public LevelData level;

    public float PlanetHealth { get; private set; }
    public float RotationDegrees { get; private set; }
    public float FuelPerSecond { get; private set; }

    /// <summary>(damage, remaining HP)</summary>
    public event System.Action<float, float> OnPlanetDamaged;
    public event System.Action OnPlanetDestroyed;

    private class Slot
    {
        public TowerKind Kind;
        public IDamageable Tower;
        public IFreezable Freezable;
    }

    private Slot[] slotTowers;
    private PlanetHull hull;

    public Transform Root { get { return root != null ? root : transform; } }
    public float Radius { get { return radius; } }

    private void Awake()
    {
        slotTowers = new Slot[Mathf.Max(1, slots)];
        hull = new PlanetHull(this);
        PlanetHealth = level != null && level.planetMaxHealth > 0f ? level.planetMaxHealth : planetMaxHealth;
    }

    private void Update()
    {
        RotationDegrees += (clockwise ? -1f : 1f) * rotationDegreesPerSecond * Time.deltaTime;
    }

    // ---------------------------------------------------------------- towers

    public void RegisterTower(int slot, TowerKind kind, IDamageable tower)
    {
        if (slotTowers == null) Awake();
        if (slot < 0 || slot >= slotTowers.Length) { Debug.LogWarning("[PlanetDefense] Slot " + slot + " is out of range."); return; }
        slotTowers[slot] = new Slot { Kind = kind, Tower = tower, Freezable = tower as IFreezable };
    }

    public void UnregisterTower(int slot)
    {
        if (slotTowers != null && slot >= 0 && slot < slotTowers.Length) slotTowers[slot] = null;
    }

    public void SetFuelPerSecond(float fuelPerSecond)
    {
        FuelPerSecond = fuelPerSecond;
    }

    public float SlotAngle(int slot)
    {
        return PlanetMath.Normalize(slot0Angle + slot * (360f / slotTowers.Length) + RotationDegrees);
    }

    public int SlotAt(float angleDegrees)
    {
        float step = 360f / slotTowers.Length;
        float rel = PlanetMath.Normalize(angleDegrees - slot0Angle - RotationDegrees);
        return Mathf.RoundToInt(rel / step) % slotTowers.Length;
    }

    private IDamageable AliveTower(int slot)
    {
        Slot s = slotTowers[slot];
        return s != null && s.Tower != null && s.Tower.IsAlive ? s.Tower : null;
    }

    // ---------------------------------------------------------------- IPlanetDefense

    public IDamageable GetTargetBelow(float angleDegrees)
    {
        IDamageable t = AliveTower(SlotAt(angleDegrees));
        return t != null ? t : hull;
    }

    public List<IDamageable> GetTargetsNear(float angleDegrees, float spreadDegrees)
    {
        var hits = new List<IDamageable>();
        int centre = SlotAt(angleDegrees);
        for (int i = 0; i < slotTowers.Length; i++)
        {
            IDamageable t = AliveTower(i);
            if (t != null && PlanetMath.Distance(SlotAngle(i), angleDegrees) <= spreadDegrees) hits.Add(t);
        }
        if (AliveTower(centre) == null) hits.Add(hull);
        return hits;
    }

    public bool FreezeNearestTower(float angleDegrees, float seconds)
    {
        int best = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < slotTowers.Length; i++)
        {
            if (AliveTower(i) == null || slotTowers[i].Freezable == null) continue;
            float d = PlanetMath.Distance(SlotAngle(i), angleDegrees);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        if (best < 0) return false;
        slotTowers[best].Freezable.Freeze(seconds);
        return true;
    }

    public DefenseSnapshot BuildSnapshot()
    {
        var snap = new DefenseSnapshot { FuelPerSecond = FuelPerSecond, PlanetHp = PlanetHealth };
        for (int i = 0; i < slotTowers.Length; i++)
        {
            if (AliveTower(i) == null) continue;
            snap.Towers.Add(new TowerInfo { Kind = slotTowers[i].Kind, AngleDeg = SlotAngle(i), IsAlive = true });
        }
        return snap;
    }

    // ---------------------------------------------------------------- planet HP

    private class PlanetHull : IDamageable
    {
        private readonly SimplePlanetDefense owner;
        public PlanetHull(SimplePlanetDefense owner) { this.owner = owner; }
        public bool IsAlive { get { return owner.PlanetHealth > 0f; } }

        public float ReceiveDamage(float amount)
        {
            if (amount <= 0f || !IsAlive) return 0f;
            float dealt = Mathf.Min(amount, owner.PlanetHealth);
            owner.PlanetHealth -= dealt;
            if (owner.level != null) owner.level.PlanetHealth = owner.PlanetHealth;
            if (owner.OnPlanetDamaged != null) owner.OnPlanetDamaged(dealt, owner.PlanetHealth);
            if (owner.PlanetHealth <= 0f && owner.OnPlanetDestroyed != null) owner.OnPlanetDestroyed();
            return dealt;
        }
    }
}
