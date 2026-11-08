using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Rigidbody2D is already required by GameEntity
public class TowerProjectile : GameEntity, IEnemyHitter
{
    [Tooltip("Units per second.")]
    public float speed = 5f;
    [Tooltip("How long before it destroys itself if it misses.")]
    public float maxLifeTime = 3f;

    // --- IEnemyHitter Interface ---
    public float Damage { get; private set; }
    public bool IgnoresShield { get; private set; }
    public bool CanHitStealth { get; private set; }

    private BaseTowerData sourceTower;
    private BaseEnemyData target;
    private float lifeTimer;
    private Vector2 moveDirection;

    /// <summary>Clean up when returned to the PoolManager.</summary>
    protected override void OnDespawned()
    {
        base.OnDespawned();
        sourceTower = null;
        target = null;
    }

    /// <summary>Call this from the Tower's Fire() method.</summary>
    public void Launch(BaseTowerData tower, BaseEnemyData enemyTarget, Vector2 spawnPosition)
    {
        sourceTower = tower;
        target = enemyTarget;

        // Copy stats from the tower so the projectile knows how hard to hit
        Damage = tower.Damage;
        IgnoresShield = tower.IgnoresShield;
        CanHitStealth = tower.CanHitStealth;

        lifeTimer = maxLifeTime;

        // Teleport to the cannon's barrel without sweeping physics
        body.position = spawnPosition;

        // Initial rotation
        if (target != null)
        {
            moveDirection = ((Vector2)target.transform.position - body.position).normalized;
            FaceDirection(moveDirection);
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned) return;

        lifeTimer -= Time.fixedDeltaTime;
        if (lifeTimer <= 0f)
        {
            Despawn();
            return;
        }

        // Homing: If the target is still alive, update the direction
        if (target != null && target.IsAlive)
        {
            moveDirection = (target.Body.position - body.position).normalized;
            FaceDirection(moveDirection);
        }

        // Move forward kinematically
        body.MovePosition(body.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    private void FaceDirection(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        body.MoveRotation(angle);
    }

    /// <summary>
    /// Triggered by BaseEnemyData when this projectile's collider hits it.
    /// </summary>
    public void OnHitEnemy(BaseEnemyData enemy)
    {
        // Pass the event back to the tower in case it applies debuffs (like poison/slows)
        if (sourceTower != null)
        {
            sourceTower.OnHitEnemy(enemy);
        }

        // The enemy script has already subtracted health at this point, so we just despawn.
        Despawn();
    }
}