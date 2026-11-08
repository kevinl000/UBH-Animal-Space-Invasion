using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BaseTowerData : GameEntity, IDamageable, IFreezable
{
    // --- State ---
    public static readonly List<BaseTowerData> Active = new List<BaseTowerData>();
    public bool IsDisabled { get; private set; } // True when out of fuel or EMP'd

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
    protected float fuelTimer;
    private bool dead;

    // --- Properties ---
    public float Health { get { return currentHealth; } }
    public float MaxHealth { get { return maxHealth; } }
    public bool IsAlive { get { return IsSpawned && !dead && currentHealth > 0f; } }

    // We keep these properties so the tower can pass them to its projectiles, 
    // but they are no longer tied to the IEnemyHitter interface.
    public float Damage { get { return damage; } }
    /// <summary>What this tower's projectile deals on contact. Towers whose damage comes from OnHitEnemy (Pest Trap DoT) return 0.</summary>
    public virtual float ProjectileDamage { get { return damage; } }
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
        fireTimer = fireRate;
        fuelTimer = 0f;
        IsDisabled = false;

        fireTimer = fireRate > 0f ? Random.Range(0f, fireRate * 0.2f) : 0f;

        TowerEvents.RaiseTowerBuilt(cost);
        OnInitialized();
    }

    /// <summary>Hook for subclasses.</summary>
    protected virtual void OnInitialized() { }

    // ---------------------------------------------------------------- logic loop

    protected virtual void Update()
    {
        if (!IsAlive || data == null) return;

        float dt = Time.deltaTime;

        ConsumeFuel(dt);

        if (freezeTimer > 0f)
        {
            freezeTimer -= dt;
            return;
        }

        if (IsDisabled) return;

        Tick(dt);
    }

    /// <summary>
    /// Accumulates time and applies the Fuel Intake per second.
    /// </summary>
    protected virtual void ConsumeFuel(float dt)
    {
        if (fuelIntake == 0) return; // Free towers don't do anything here

        fuelTimer += dt;
        if (fuelTimer >= 1f)
        {
            fuelTimer -= 1f; // Remove 1 second from the timer

            // Tell the economy manager to add/subtract this fuel amount
            TowerEvents.RaiseTowerFuelTick(fuelIntake);

            // NOTE: If you want towers to stop firing when you run out of fuel,
            // your EconomyManager could disable the tower, or you could check a 
            // global GameManager.HasFuel boolean here before letting Tick() run.
        }
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

    /// <summary>
    /// README FOV is an angle: the enemy must be within fov/2 degrees of the tower's outward direction (transform.up,
    /// which LevelPlanetDefense points away from the planet). data.range limits the distance (0 = unlimited).
    /// </summary>
    public bool InSight(BaseEnemyData enemy)
    {
        if (enemy == null || enemy.Body == null) return false;
        Vector2 toEnemy = enemy.Body.position - (Vector2)transform.position;
        if (data != null && data.range > 0f && toEnemy.magnitude > data.range) return false;
        if (fov <= 0f || fov >= 360f) return true;
        return Vector2.Angle(transform.up, toEnemy) <= fov * 0.5f;
    }

    // ---------------------------------------------------------------- interfaces
    /// <summary>
    /// Called by the TowerProjectile when it hits an enemy.
    /// Override this in subclasses to apply debuffs (e.g., enemy.ApplySlow()).
    /// </summary>

    public virtual void SetDisabled(bool disabled)
    {
        IsDisabled = disabled;

        // Hook: You could change the sprite color here to grey it out,
        // or stop a spinning radar animation when disabled.
    }

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
        TowerEvents.RaiseTowerSold(refundAmount);

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

    protected override void OnSpawned()
    {
        base.OnSpawned();
        currentHealth = maxHealth;
        dead = false;
        freezeTimer = 0f;
        fireTimer = fireRate;
        fuelTimer = 0f;
        IsDisabled = false; // Always start enabled

        if (!Active.Contains(this)) Active.Add(this);
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();
        dead = true;
        Active.Remove(this);
    }
}