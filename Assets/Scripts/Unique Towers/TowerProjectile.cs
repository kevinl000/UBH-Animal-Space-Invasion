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
    public PooledEffect impactEffect;
    private int targetSpawnId;
    private int sourceSpawnId;
    private float areaRadius;
    private bool consumed;
    private float dotDamage, dotDuration, slowFactor, percentDamage;
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
        targetSpawnId = target != null ? target.SpawnId : 0;
        sourceSpawnId = tower.SpawnId;
        consumed = false;
        PestTrapTower trap = tower as PestTrapTower;
        dotDamage = trap != null ? trap.Damage : 0f;
        dotDuration = trap != null ? trap.slowDuration : 0f;
        slowFactor = trap != null ? trap.slowAmount : 1f;
        BallisticaTower ballistica = tower as BallisticaTower;
        percentDamage = ballistica != null ? ballistica.percentDamage : 0f;
        AoETower area = tower as AoETower;
        areaRadius = area != null ? area.effectRadius : 0f;

        // Copy stats from the tower
        Damage = tower.ProjectileDamage;
        IgnoresShield = tower.IgnoresShield;
        CanHitStealth = tower.CanHitStealth;

        lifeTimer = maxLifeTime;
        moveDirection = Vector2.right;

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
        if (!IsSpawned || !GameplayLoopController.CombatAllowed) return;

        lifeTimer -= Time.fixedDeltaTime;
        if (lifeTimer <= 0f)
        {
            Despawn();
            return;
        }

        // Homing logic: update direction if the target is still alive
        if (target != null && target.IsAlive && target.SpawnId == targetSpawnId)
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

    // Reserve the hit before damage: death callbacks and multiple contacts cannot reuse it.
    public void TryHit(BaseEnemyData enemy)
    {
        if (!IsSpawned || consumed || !GameplayLoopController.CombatAllowed || !enemy.IsTargetable(CanHitStealth)) return;
        consumed = true;
        Vector3 hitAt = enemy.transform.position;
        Vector3 contact = enemy.Collider != null ? (Vector3)enemy.Collider.ClosestPoint(body.position) : hitAt;
        if (IgnoresShield) enemy.TakeDamageIgnoringShield(Damage);
        else enemy.TakeDamage(Damage);
        if (areaRadius > 0f)
        {
            for (int i = BaseEnemyData.Active.Count - 1; i >= 0; i--)
            {
                BaseEnemyData other = BaseEnemyData.Active[i];
                if (other == enemy || !other.IsTargetable(CanHitStealth) || Vector2.Distance(other.Body.position, hitAt) > areaRadius) continue;
                if (IgnoresShield) other.TakeDamageIgnoringShield(Damage);
                else other.TakeDamage(Damage);
            }
        }
        else if (dotDuration > 0f && enemy.IsAlive)
        {
            enemy.ApplySlow(dotDuration, slowFactor);
            enemy.ApplyDamageOverTime(dotDamage, dotDuration);
        }
        else if (percentDamage > 0f && enemy.IsAlive)
        {
            if (IgnoresShield) enemy.TakeDamageIgnoringShield(enemy.MaxHealth * percentDamage);
            else enemy.TakeDamage(enemy.MaxHealth * percentDamage);
        }
        else if (sourceTower != null && sourceTower.IsAlive && sourceTower.SpawnId == sourceSpawnId)
            sourceTower.OnHitEnemy(enemy);
        float scale = areaRadius > 0f ? areaRadius : 1f;
        CombatVisual.Play(impactEffect, areaRadius > 0f ? hitAt : contact, body.rotation, null, scale);
        Despawn();
    }

    public void OnHitEnemy(BaseEnemyData enemy) { Despawn(); }
}
