using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BaseTowerData : GameEntity, IDamageable, IFreezable
{
    // --- The Scriptable Object ---
    public TowerData data;

    // --- Local Stats (Copied from data so they can be buffed/debuffed independently) ---
    public float maxHealth;
    public int cost;
    public int fuelIntake;
    public float fov;
    public float damage;
    public float fireRate;
    public bool ignoresShield;
    public bool canHitStealth;

    // --- State ---
    protected float currentHealth;
    protected float fireTimer;
    protected float freezeTimer;
    private bool dead;

    // --- Properties ---
    public float Health { get { return currentHealth; } }
    public float MaxHealth { get { return maxHealth; } }
    public bool IsAlive { get { return IsSpawned && !dead && currentHealth > 0f; } }

    // We keep these properties so the tower can pass them to its projectiles, 
    // but they are no longer tied to the IEnemyHitter interface.
    public float Damage { get { return damage; } }
    public bool IgnoresShield { get { return ignoresShield; } }
    public bool CanHitStealth { get { return canHitStealth; } }

    /// <summary>Call this when placing the tower.</summary>
    public virtual void Initialize(TowerData towerData)
    {
        data = towerData;

        maxHealth = data != null ? data.maxHealth : maxHealth;
        cost = data != null ? data.cost : cost;
        fuelIntake = data != null ? data.fuelIntake : fuelIntake;
        fov = data != null ? data.fov : fov;
        damage = data != null ? data.damage : damage;
        fireRate = data != null ? data.fireRate : fireRate;
        ignoresShield = data != null ? data.ignoresShield : ignoresShield;
        canHitStealth = data != null ? data.canHitStealth : canHitStealth;

        currentHealth = maxHealth;
        dead = false;
        freezeTimer = 0f;

        fireTimer = fireRate > 0f ? Random.Range(0f, fireRate * 0.2f) : 0f;

        TowerEvents.RaiseTowerBuilt(this, cost);
        OnInitialized();
    }

    /// <summary>Hook for subclasses.</summary>
    protected virtual void OnInitialized() { }

    // ---------------------------------------------------------------- logic loop

    protected virtual void Update()
    {
        if (!IsAlive || data == null) return;

        float dt = Time.deltaTime;

        if (freezeTimer > 0f)
        {
            freezeTimer -= dt;
            return;
        }

        Tick(dt);
    }

    protected virtual void Tick(float dt)
    {
        if (data.attackType == TowerAttackType.None || fireRate <= 0) return;

        fireTimer -= dt;
        if (fireTimer <= 0f)
        {
            fireTimer = fireRate;
            Fire();
        }
    }

    /// <summary>Hook for subclasses to define how they shoot.</summary>
    protected virtual void Fire() { }

    // ---------------------------------------------------------------- interfaces
    /// <summary>
    /// Called by the TowerProjectile when it hits an enemy.
    /// Override this in subclasses to apply debuffs (e.g., enemy.ApplySlow()).
    /// </summary>
    public virtual void OnHitEnemy(BaseEnemyData enemy)
    {
        // Default towers just deal raw damage (which is handled by the projectile itself),
        // so this base method is intentionally empty.
    }
    public float ReceiveDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return 0f;

        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Die();
        }
        return amount;
    }

    public void Freeze(float seconds)
    {
        if (!IsAlive) return;
        freezeTimer = Mathf.Max(freezeTimer, seconds);
    }

    // ---------------------------------------------------------------- selling & death

    public virtual void Sell()
    {
        if (!IsAlive) return;

        dead = true;
        int refundAmount = Mathf.FloorToInt(cost * 0.5f);
        TowerEvents.RaiseTowerSold(this, refundAmount);

        OnSold();
        Despawn();
    }

    protected virtual void OnSold() { }

    protected virtual void Die()
    {
        if (dead) return;
        dead = true;

        TowerEvents.RaiseTowerDestroyed(this);

        OnDeath();
        Despawn();
    }

    protected virtual void OnDeath() { }

    protected override void OnDespawned()
    {
        base.OnDespawned();
        dead = true;
    }
}