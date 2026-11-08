using UnityEngine;

[RequireComponent(typeof(Collider2D))] // GameEntity handles Rigidbody2D
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

        // Copy stats from the tower
        Damage = tower.ProjectileDamage;
        IgnoresShield = tower.IgnoresShield;
        CanHitStealth = tower.CanHitStealth;

        lifeTimer = maxLifeTime;

        // Teleport to the spawn position (e.g., the tower's barrel)
        body.position = spawnPosition;

        // Initial rotation
        if (target != null)
        {
            moveDirection = ((Vector2)target.Body.position - body.position).normalized;
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

        // Homing logic: update direction if the target is still alive
        if (target != null && target.IsAlive)
        {
            moveDirection = (target.Body.position - body.position).normalized;
            FaceDirection(moveDirection);
        }

        body.MovePosition(body.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    private void FaceDirection(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        body.MoveRotation(angle);
    }

    // --- IEnemyHitter Implementation ---

    /// <summary>Triggered by BaseEnemyData's OnTriggerEnter2D.</summary>
    public void OnHitEnemy(BaseEnemyData enemy)
    {
        // 1. Let the tower know it hit something, in case the tower applies Poison/Slows
        if (sourceTower != null && sourceTower.IsAlive)
        {
            sourceTower.OnHitEnemy(enemy);
        }

        // 2. The enemy's OnTriggerEnter2D has already subtracted the health at this point,
        // so the projectile's only remaining job is to destroy itself.
        Despawn();
    }
}