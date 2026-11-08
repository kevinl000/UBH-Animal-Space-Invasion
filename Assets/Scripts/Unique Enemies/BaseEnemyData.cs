using System.Collections.Generic;
using UnityEngine;

/// <summary>What the spawner hands an enemy when it's created.</summary>
public class EnemySpawnContext
{
    public IPlanetDefense Planet;
    public EnemySpawner Spawner;          // so bosses can summon minions; may be null in tests
    public WaveDirectorBridge Bridge;     // enemy AI; may be null
    public int Ticket;                    // AI learning ticket (0 = not tracked)
    public float AngleDegrees;
    public float SpawnDistance = 4f;      // planet radii
    public bool IsShiny;
    public bool Orbits;
}

/// <summary>
/// Base for every enemy. Handles README stats, approach, orbiting, attacking the slot below,
/// shields, stealth, shiny, slows, drops, and reporting to the enemy AI.
/// Subclasses override Attack(), Tick() or OnDeath() for their special behaviour.
/// </summary>
public class BaseEnemyData : MonoBehaviour
{
    public EnemyData data;
    public float maxHealth;
    public float speed;
    public float damage;

    protected float currentHealth;

    /// <summary>All living enemies, for towers to pick targets from (use IsTargetable).</summary>
    public static readonly List<BaseEnemyData> Active = new List<BaseEnemyData>();

    public float Health { get { return currentHealth; } }
    public float MaxHealth { get { return maxHealth; } }
    public bool IsShiny { get; private set; }
    public bool HasShield { get; protected set; }
    public bool IsStealthed { get; protected set; }
    public bool IsInvulnerable { get; protected set; }
    public bool IsAlive { get { return !dead && currentHealth > 0f; } }
    /// <summary>Angle around the planet (degrees, counter-clockwise from +X).</summary>
    public float AngleDegrees { get; protected set; }
    /// <summary>Distance from the planet centre, in planet radii.</summary>
    public float Distance { get; protected set; }
    public bool Arrived { get; protected set; }
    public bool Orbits { get; protected set; }

    protected IPlanetDefense planet;
    protected EnemySpawnContext context;
    protected float attackTimer;
    private float slowTimer;
    private float slowFactor = 1f;
    private bool dead;
    private bool reported;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>Kept for compatibility. Prefer Initialize(data, context) from the spawner.</summary>
    public virtual void Initialize(EnemyData enemyData)
    {
        Initialize(enemyData, null);
    }

    public virtual void Initialize(EnemyData enemyData, EnemySpawnContext ctx)
    {
        data = enemyData;
        context = ctx ?? new EnemySpawnContext();
        planet = context.Planet;

        maxHealth = data != null ? data.maxHealth : maxHealth;
        speed = data != null ? data.moveSpeed : speed;
        damage = data != null ? data.damage : damage;

        IsShiny = context.IsShiny;
        if (IsShiny) maxHealth *= 2f; // README: shiny = double HP
        currentHealth = maxHealth;

        HasShield = data != null && data.hasShield;
        IsStealthed = data != null && data.isStealth;
        Orbits = context.Orbits || (data != null && data.movement != EnemyMovement.StopAndShoot);
        AngleDegrees = PlanetMath.Normalize(context.AngleDegrees);
        Distance = context.SpawnDistance;
        Arrived = false;
        attackTimer = data != null ? Random.Range(0f, data.attackInterval) : 1f; // don't all fire on the same frame

        if (!Active.Contains(this)) Active.Add(this);
        Place();
        OnInitialized();
        EnemyEvents.RaiseSpawned(this);
    }

    /// <summary>Hook for subclasses after stats are set.</summary>
    protected virtual void OnInitialized() { }

    protected virtual void Update()
    {
        if (!IsAlive || data == null || planet == null) return;
        float dt = Time.deltaTime;
        if (slowTimer > 0f)
        {
            slowTimer -= dt;
            dt *= slowFactor;
            if (slowTimer <= 0f) slowFactor = 1f;
        }
        Tick(dt);
        Place();
    }

    /// <summary>One step of behaviour. Default: move, then attack on a timer once arrived.</summary>
    protected virtual void Tick(float dt)
    {
        Move(dt);
        if (Arrived) TickAttack(dt);
    }

    protected float TargetDistance
    {
        get { return Orbits ? data.orbitDistance : data.stopDistance; }
    }

    protected void Move(float dt)
    {
        if (Distance > TargetDistance)
        {
            Distance = Mathf.Max(TargetDistance, Distance - speed * dt);
        }
        else
        {
            Arrived = true;
        }

        if (Orbits && Arrived)
        {
            AngleDegrees = PlanetMath.Normalize(AngleDegrees + data.orbitDegreesPerSecond * data.orbitSpeed * dt);
        }
    }

    protected void TickAttack(float dt)
    {
        if (data.attackType == EnemyAttackType.None) return;
        attackTimer -= dt;
        if (attackTimer > 0f) return;
        attackTimer = data.attackInterval;
        Attack();
    }

    /// <summary>Default attack: shoot the slot below (AoE also hits neighbours).</summary>
    protected virtual void Attack()
    {
        FireAt(AngleDegrees, damage, data.attackType == EnemyAttackType.AoE);
    }

    /// <summary>Sends a projectile (if the data has one) or hits instantly.</summary>
    protected void FireAt(float angleDegrees, float amount, bool aoe)
    {
        if (data.projectilePrefab != null && planet != null)
        {
            EnemyProjectile p = Instantiate(data.projectilePrefab, planet.Root);
            p.Launch(planet, transform.localPosition, angleDegrees, () => HitAt(angleDegrees, amount, aoe));
        }
        else
        {
            HitAt(angleDegrees, amount, aoe);
        }
    }

    /// <summary>Deals damage at an angle on the planet and reports it to the AI. Returns damage dealt.</summary>
    protected float HitAt(float angleDegrees, float amount, bool aoe)
    {
        if (planet == null) return 0f;
        float dealt = 0f;
        if (aoe)
        {
            foreach (IDamageable t in planet.GetTargetsNear(angleDegrees, data.aoeSpreadDegrees))
                if (t != null && t.IsAlive) dealt += t.ReceiveDamage(amount);
        }
        else
        {
            IDamageable t = planet.GetTargetBelow(angleDegrees);
            if (t != null && t.IsAlive) dealt += t.ReceiveDamage(amount);
        }
        if (dealt > 0f && context.Bridge != null) context.Bridge.ReportDamage(context.Ticket, dealt);
        return dealt;
    }

    // ---------------------------------------------------------------- taking damage

    /// <summary>For towers: can this tower target the enemy?</summary>
    public bool IsTargetable(bool canSeeStealth)
    {
        return IsAlive && (!IsStealthed || canSeeStealth);
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (!IsAlive || IsInvulnerable || damageAmount <= 0f) return;
        if (HasShield)
        {
            HasShield = false; // README: the shield negates the first incoming damage
            OnShieldBroken();
            return;
        }
        currentHealth -= damageAmount;
        if (currentHealth <= 0f) Die();
    }

    /// <summary>Damage over time that ignores the shield (Pest Trap).</summary>
    public void TakeDamageIgnoringShield(float damageAmount)
    {
        if (!IsAlive || IsInvulnerable || damageAmount <= 0f) return;
        currentHealth -= damageAmount;
        if (currentHealth <= 0f) Die();
    }

    /// <summary>Slows movement and attacks, e.g. Pest Trap (factor 0.5 = half speed).</summary>
    public void ApplySlow(float seconds, float factor)
    {
        slowTimer = Mathf.Max(slowTimer, seconds);
        slowFactor = Mathf.Clamp01(factor);
    }

    protected virtual void OnShieldBroken() { }

    protected virtual void Heal(float amount)
    {
        if (IsAlive) currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    // ---------------------------------------------------------------- death

    protected virtual void Die()
    {
        if (dead) return;
        dead = true;
        Active.Remove(this);
        ReportGone();

        int mult = IsShiny ? 2 : 1; // README: shiny = double money / fuel
        int money = data != null ? data.moneyDrop * mult : 0;
        int fuel = data != null ? data.fuelDrop * mult : 0;
        EnemyEvents.RaiseKilled(this, money, fuel);

        OnDeath();
        Destroy(gameObject);
    }

    /// <summary>Hook for death effects (e.g. Animal Ship releasing animals).</summary>
    protected virtual void OnDeath() { }

    protected virtual void OnDestroy()
    {
        Active.Remove(this);
        ReportGone(); // despawned without dying (level end): still counts for learning
    }

    private void ReportGone()
    {
        if (reported) return;
        reported = true;
        if (context != null && context.Bridge != null) context.Bridge.ReportGone(context.Ticket);
    }

    // ---------------------------------------------------------------- helpers

    protected void Place()
    {
        if (planet == null) return;
        transform.localPosition = PlanetMath.ToLocal(AngleDegrees, Distance, planet.Radius);
        // face the planet centre (sprites drawn facing up)
        transform.localRotation = Quaternion.Euler(0f, 0f, AngleDegrees + 90f);
    }

    /// <summary>Summons another enemy next to this one (bosses). Returns null without a spawner.</summary>
    protected BaseEnemyData Summon(EnemyData minion, float angleOffset)
    {
        if (minion == null || context.Spawner == null) return null;
        return context.Spawner.SpawnMinion(minion, AngleDegrees + angleOffset, Mathf.Max(Distance, minion.stopDistance + 0.5f));
    }
}
