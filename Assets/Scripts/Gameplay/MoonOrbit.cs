using System.Collections.Generic;
using UnityEngine;
using EnemyAI;

/// <summary>
/// Silverman Luna (README): a moon with 4 tower slots that orbits the planet and spins.
///  - The moon art (with a CircleCollider2D) gets a kinematic Rigidbody2D and is moved with MovePosition / MoveRotation
///    in FixedUpdate. It keeps its authored distance from the planet, measured in planet radii, so the HUD zoom still fits.
///  - Towers built on the moon ride its slots. Build with TryPlaceTower (TowerPlacementInput does this when you click the moon).
///  - Moon towers shoot like any tower and count in the enemy AI's view of the defense. Enemies only attack the planet itself.
/// </summary>
public class MoonOrbit : MonoBehaviour
{
    public LevelPlanetDefense planet;
    [Tooltip("The moon art object (Silverman Luna: 'Moon', with a CircleCollider2D).")]
    public Transform moon;
    [Tooltip("Degrees per second around the planet (positive = counter-clockwise).")]
    public float orbitDegreesPerSecond = 8f;
    [Tooltip("Degrees per second the moon spins on itself.")]
    public float spinDegreesPerSecond = -20f;
    [Tooltip("README: only 4 slots on the moon.")]
    public int slots = 4;
    public float slot0Angle = 90f;
    [Tooltip("Where towers sit, in moon radii from its centre.")]
    public float towerDistance = 1f;

    public float OrbitAngle { get; private set; }
    public float SpinDegrees { get; private set; }
    public float MoonRadius { get; private set; }
    /// <summary>The moon's centre in the gameplay (camera) world: where its towers sit.</summary>
    public Vector2 Center { get; private set; }
    /// <summary>The moon's centre in its own (UI) space.</summary>
    public Vector3 CenterUI { get; private set; }

    private struct Slot { public BaseTowerData Tower; public int SpawnId; }
    private Slot[] slotTowers = new Slot[0];
    private Rigidbody2D body;
    private float orbitRadii;   // distance from the planet centre, in planet radii (UI space)
    private bool started;

    private void Awake()
    {
        if (planet == null) planet = FindAnyObjectByType<LevelPlanetDefense>();
        slotTowers = new Slot[Mathf.Max(1, slots)];
        if (moon == null) { Debug.LogWarning("[MoonOrbit] No moon assigned."); return; }
        body = moon.GetComponent<Rigidbody2D>();
        if (body == null) body = moon.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;  // its own body, not part of the planet's
        body.useFullKinematicContacts = true;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.None; // the parent (planet) moves too; don't let two smoothings fight
        // as its own body the moon would push the (dynamic) planet away: make its colliders triggers
        foreach (Collider2D c in moon.GetComponents<Collider2D>()) c.isTrigger = true;
    }

    private float MeasureRadius()
    {
        CircleCollider2D c = moon.GetComponent<CircleCollider2D>();
        if (c == null) return 0.5f;
        Vector3 s = moon.lossyScale;
        return c.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y)) * UiSpace.Scale(moon); // gameplay units
    }

    private void FixedUpdate()
    {
        if (moon == null || planet == null || planet.Radius <= 0f || planet.PlanetRadiusUI <= 0f) return;
        // the moon is UI like the planet: orbit in their (UI) space
        Vector2 centre = planet.PlanetCenterUI;
        if (!started)
        {
            // keep the authored layout: start where the artist put the moon
            Vector2 offset = (Vector2)moon.position - centre;
            orbitRadii = offset.magnitude / planet.PlanetRadiusUI;
            if (orbitRadii < 0.01f) orbitRadii = 1.6f;
            OrbitAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            SpinDegrees = moon.eulerAngles.z;
            started = true;
        }

        float dt = Time.fixedDeltaTime;
        OrbitAngle = PlanetMath.Normalize(OrbitAngle + orbitDegreesPerSecond * dt);
        SpinDegrees = PlanetMath.Normalize(SpinDegrees + spinDegreesPerSecond * dt);
        MoonRadius = MeasureRadius();
        Vector2 centerUI = centre + (Vector2)PlanetMath.ToLocal(OrbitAngle, orbitRadii, planet.PlanetRadiusUI);
        CenterUI = new Vector3(centerUI.x, centerUI.y, moon.position.z);
        Center = UiSpace.ToGameplay(moon, CenterUI);
        body.MovePosition(centerUI);
        body.MoveRotation(SpinDegrees);

        for (int i = 0; i < slotTowers.Length; i++)
        {
            BaseTowerData t = TowerIn(i);
            if (t == null) { slotTowers[i] = new Slot(); continue; }
            if (t.Body == null) continue;
            float a = SlotAngle(i);
            t.Body.MovePosition(Center + (Vector2)PlanetMath.ToLocal(a, towerDistance, MoonRadius));
            t.Body.MoveRotation(a - 90f); // faces away from the moon
        }
    }

    // ---------------------------------------------------------------- slots

    public int SlotCount { get { return slotTowers.Length; } }

    public float SlotAngle(int slot)
    {
        return PlanetMath.Normalize(slot0Angle + slot * (360f / slotTowers.Length) + SpinDegrees);
    }

    public int SlotAt(float angleDegrees)
    {
        float step = 360f / slotTowers.Length;
        return Mathf.RoundToInt(PlanetMath.Normalize(angleDegrees - slot0Angle - SpinDegrees) / step) % slotTowers.Length;
    }

    /// <summary>True if a world point is on / near the moon; slot = the closest moon slot.</summary>
    public bool TryGetSlotAt(Vector2 world, out int slot, float maxRadii = 1.8f)
    {
        slot = -1;
        if (!started || MoonRadius <= 0f) return false;
        Vector2 d = world - Center;
        if (d.magnitude > MoonRadius * maxRadii) return false;
        slot = SlotAt(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        return true;
    }

    public BaseTowerData TowerIn(int slot)
    {
        if (slot < 0 || slot >= slotTowers.Length) return null;
        Slot s = slotTowers[slot];
        if (s.Tower == null || !s.Tower.IsAlive || s.Tower.SpawnId != s.SpawnId) return null;
        return s.Tower;
    }

    public IEnumerable<BaseTowerData> Towers()
    {
        for (int i = 0; i < slotTowers.Length; i++)
        {
            BaseTowerData t = TowerIn(i);
            if (t != null) yield return t;
        }
    }

    public bool TryPlaceTower(TowerData data, int slot, out string reason)
    {
        reason = null;
        if (!started) { reason = "The moon isn't ready yet."; return false; }
        if (data == null || data.prefab == null) { reason = "This tower has no prefab yet."; return false; }
        if (slot < 0 || slot >= slotTowers.Length) { reason = "No moon slot there."; return false; }
        if (TowerIn(slot) != null) { reason = "That moon slot already has a tower."; return false; }
        LevelManager money = planet != null ? planet.levelManager : null;
        if (money != null && !money.CanAfford(data.cost)) { reason = "Not enough money ($" + data.cost + ")."; return false; }
        if (PoolManager.Instance == null) { reason = "No PoolManager in the scene."; return false; }

        float a = SlotAngle(slot);
        BaseTowerData tower = PoolManager.Instance.Spawn(data.prefab, planet != null ? planet.Root : transform,
            Center + (Vector2)PlanetMath.ToLocal(a, towerDistance, MoonRadius), a - 90f);
        tower.name = data.name + " (moon)";
        tower.Initialize(data); // LevelManager pays
        slotTowers[slot] = new Slot { Tower = tower, SpawnId = tower.SpawnId };
        return true;
    }

    public bool SellTower(int slot)
    {
        BaseTowerData t = TowerIn(slot);
        if (t == null) return false;
        t.Sell();
        slotTowers[slot] = new Slot();
        return true;
    }
}
