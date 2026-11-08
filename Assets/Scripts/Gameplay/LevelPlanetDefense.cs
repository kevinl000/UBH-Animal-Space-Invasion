using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EnemyAI;

/// <summary>
/// Connects the level together: the rotating planet (LevelManager), the tower slots, and the enemies.
///  - Enemies hit the tower in the slot below them, or the planet (LevelManager.ReceiveDamage) if the slot is empty.
///  - Towers are placed with TryPlaceTower(towerData, slot): money is checked, the tower comes from the PoolManager,
///    and it rides its slot as the planet turns (Rigidbody2D MovePosition / MoveRotation in FixedUpdate).
///  - When the level time is up and every enemy is gone, GameManager.LevelComplete() is called.
/// Lives on its own unrotated GameObject; it follows the planet's centre. Enemies and towers are parented to it.
/// </summary>
public class LevelPlanetDefense : MonoBehaviour, IPlanetDefense
{
    [Header("Scene references")]
    public LevelManager levelManager;
    [Tooltip("The planet object that rotates (LevelManager.planetBody). Empty = take it from the LevelManager.")]
    public Transform planet;
    public EnemySpawner spawner;

    [Header("Slots")]
    [Tooltip("README: small 6, small-medium 8, medium 12, big 16.")]
    public int slots = 12;
    [Tooltip("Angle of slot 0 before rotation (90 = top).")]
    public float slot0Angle = 90f;
    [Tooltip("Planet radius in world units. 0 = from the planet's CircleCollider2D.")]
    public float radius;
    [Tooltip("Where towers sit, in planet radii from the centre (1 = on the surface).")]
    public float towerDistance = 1.0f;

    public float RotationDegrees { get; private set; }
    public float FuelPerSecond { get; private set; }
    public int SlotCount { get { return slotTowers.Length; } }

    public Transform Root { get { return transform; } }
    public float Radius { get { return radius; } }

    private struct Slot
    {
        public BaseTowerData Tower;
        public int SpawnId;
    }

    private Slot[] slotTowers = new Slot[0];
    private PlanetHull hull;
    private bool timeUp;
    private bool completed;
    private bool autoRadius;
    private bool warnedRadius;

    private void Awake()
    {
        if (levelManager == null) levelManager = FindAnyObjectByType<LevelManager>();
        if (planet == null && levelManager != null && levelManager.planetBody != null) planet = levelManager.planetBody.transform;
        if (spawner == null) spawner = FindAnyObjectByType<EnemySpawner>();
        slotTowers = new Slot[Mathf.Max(1, slots)];
        hull = new PlanetHull(this);
        autoRadius = radius <= 0f;
        if (autoRadius) radius = MeasureRadius();
        FollowPlanet();
    }

    /// <summary>The planet's biggest CircleCollider2D (some planets also have a smaller one, e.g. a moon).</summary>
    private CircleCollider2D PlanetCollider()
    {
        if (planet == null) return null;
        CircleCollider2D best = null;
        foreach (CircleCollider2D c in planet.GetComponents<CircleCollider2D>())
            if (best == null || c.radius > best.radius) best = c;
        return best;
    }

    private float MeasureRadius()
    {
        CircleCollider2D c = PlanetCollider();
        if (c != null)
        {
            Vector3 s = planet.lossyScale;
            return c.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        }
        if (!warnedRadius) Debug.LogWarning("[LevelPlanetDefense] Couldn't measure the planet radius (no CircleCollider2D on the planet). Set 'radius' by hand.");
        warnedRadius = true;
        return radius > 0f ? radius : 2f;
    }

    private void OnEnable()
    {
        EnemyEvents.LevelTimeUp += OnLevelTimeUp;
    }

    private void OnDisable()
    {
        EnemyEvents.LevelTimeUp -= OnLevelTimeUp;
    }

    private void OnLevelTimeUp()
    {
        timeUp = true;
    }

    private void FollowPlanet()
    {
        if (planet == null) return;
        CircleCollider2D c = PlanetCollider();
        Vector3 p = c != null ? planet.TransformPoint(c.offset) : planet.position; // some planets' circles are offset from the art's pivot
        transform.position = new Vector3(p.x, p.y, 0f);
        RotationDegrees = planet.eulerAngles.z; // LevelManager turns the planet's Rigidbody2D
    }

    private void Update()
    {
        if (levelManager != null && levelManager.time != null && spawner != null && spawner.level != null && spawner.level.maxTime > 0f)
        {
            float left = Mathf.Max(0f, spawner.level.maxTime - spawner.Elapsed);
            levelManager.time.text = "TIME " + Mathf.FloorToInt(left / 60f) + ":" + Mathf.FloorToInt(left % 60f).ToString("00");
        }

        if (timeUp && !completed && BaseEnemyData.Active.Count == 0 && (levelManager == null || levelManager.IsAlive))
        {
            completed = true;
            if (GameManager.Instance != null) GameManager.Instance.LevelComplete();
        }
    }

    // Physics step: towers ride their slots as the planet turns.
    private void FixedUpdate()
    {
        FollowPlanet();
        if (autoRadius) radius = MeasureRadius(); // the HUD's zoom scales the planet
        float fuel = 0f;
        for (int i = 0; i < slotTowers.Length; i++)
        {
            BaseTowerData t = TowerIn(i);
            if (t == null) { slotTowers[i] = new Slot(); continue; }
            if (t.Body != null)
            {
                float a = SlotAngle(i);
                t.Body.MovePosition(transform.TransformPoint(PlanetMath.ToLocal(a, towerDistance, radius)));
                t.Body.MoveRotation(a - 90f); // sprite "up" points away from the planet (that's the FOV direction)
            }
            if (!t.IsDisabled) fuel += t.fuelIntake;
        }
        FuelPerSecond = fuel;
    }

    // ---------------------------------------------------------------- slots

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

    /// <summary>The living tower in this slot, or null.</summary>
    public BaseTowerData TowerIn(int slot)
    {
        if (slot < 0 || slot >= slotTowers.Length) return null;
        Slot s = slotTowers[slot];
        // a sold / destroyed tower goes back to the pool and may be reused elsewhere: check the spawn id
        if (s.Tower == null || !s.Tower.IsAlive || s.Tower.SpawnId != s.SpawnId) return null;
        return s.Tower;
    }

    /// <summary>Builds a tower in a slot. Returns false (with a reason for the UI) if it can't.</summary>
    public bool TryPlaceTower(TowerData data, int slot, out string reason)
    {
        reason = null;
        if (data == null || data.prefab == null) { reason = "This tower has no prefab yet."; return false; }
        if (slot < 0 || slot >= slotTowers.Length) { reason = "No slot there."; return false; }
        if (TowerIn(slot) != null) { reason = "That slot already has a tower."; return false; }
        if (levelManager != null && !levelManager.CanAfford(data.cost)) { reason = "Not enough money ($" + data.cost + ")."; return false; }
        if (PoolManager.Instance == null) { reason = "No PoolManager in the scene."; return false; }

        float a = SlotAngle(slot);
        BaseTowerData tower = PoolManager.Instance.Spawn(data.prefab, transform,
            transform.TransformPoint(PlanetMath.ToLocal(a, towerDistance, radius)), a - 90f);
        tower.name = data.name;
        tower.Initialize(data); // raises TowerBuilt -> LevelManager pays the cost
        slotTowers[slot] = new Slot { Tower = tower, SpawnId = tower.SpawnId };
        return true;
    }

    /// <summary>Sells the tower in this slot (half the cost back). Returns false if the slot is empty.</summary>
    public bool SellTower(int slot)
    {
        BaseTowerData t = TowerIn(slot);
        if (t == null) return false;
        t.Sell();
        slotTowers[slot] = new Slot();
        return true;
    }

    // ---------------------------------------------------------------- IPlanetDefense

    public IDamageable GetTargetBelow(float angleDegrees)
    {
        BaseTowerData t = TowerIn(SlotAt(angleDegrees));
        return t != null ? (IDamageable)t : hull;
    }

    public List<IDamageable> GetTargetsNear(float angleDegrees, float spreadDegrees)
    {
        var hits = new List<IDamageable>();
        for (int i = 0; i < slotTowers.Length; i++)
        {
            BaseTowerData t = TowerIn(i);
            if (t != null && PlanetMath.Distance(SlotAngle(i), angleDegrees) <= spreadDegrees) hits.Add(t);
        }
        if (TowerIn(SlotAt(angleDegrees)) == null) hits.Add(hull);
        return hits;
    }

    public bool FreezeNearestTower(float angleDegrees, float seconds)
    {
        int best = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < slotTowers.Length; i++)
        {
            if (TowerIn(i) == null) continue;
            float d = PlanetMath.Distance(SlotAngle(i), angleDegrees);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        if (best < 0) return false;
        TowerIn(best).Freeze(seconds);
        return true;
    }

    public DefenseSnapshot BuildSnapshot()
    {
        var snap = new DefenseSnapshot
        {
            FuelPerSecond = FuelPerSecond,
            PlanetHp = levelManager != null ? levelManager.PlanetHP : 0f,
            Fuel = levelManager != null ? levelManager.TotalFuel : 0f,
            Money = levelManager != null ? levelManager.TotalMoney : 0f,
        };
        for (int i = 0; i < slotTowers.Length; i++)
        {
            BaseTowerData t = TowerIn(i);
            if (t == null || t.IsDisabled) continue; // a tower without fuel doesn't cover anything
            snap.Towers.Add(new TowerInfo { Kind = t.data != null ? t.data.kind : TowerKind.Laser, AngleDeg = SlotAngle(i), Hp = t.Health, IsAlive = true });
        }
        return snap;
    }

    /// <summary>Damage to the planet itself goes to the LevelManager's HP.</summary>
    private class PlanetHull : IDamageable
    {
        private readonly LevelPlanetDefense owner;
        public PlanetHull(LevelPlanetDefense owner) { this.owner = owner; }
        public bool IsAlive { get { return owner.levelManager == null || owner.levelManager.IsAlive; } }
        public float ReceiveDamage(float amount)
        {
            return owner.levelManager != null ? owner.levelManager.ReceiveDamage(amount) : 0f;
        }
    }
}
