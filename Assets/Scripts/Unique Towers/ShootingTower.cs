using UnityEngine;

public class ShootingTower : BaseTowerData
{
    protected override void Tick(float dt)
    {
        if (Visual != null)
        {
            BaseEnemyData target = FindClosestEnemy();
            if (target != null) Visual.Aim(target.Body.position);
        }
        base.Tick(dt);
    }

    protected override void Fire()
    {
        BaseEnemyData target = FindClosestEnemy();
        if (target == null) return; // Nobody in range
        if (data.projectilePrefab != null && PoolManager.Instance == null) return;
        if (!TryConsumeShotFuel()) return;
        if (Visual != null) { Visual.Aim(target.Body.position); Visual.Attack(); }
        Vector3 origin = Visual != null && Visual.Muzzle != null ? Visual.Muzzle.position : transform.position;

        if (data.projectilePrefab != null)
        {
            // FIX: Pass planet.Root as the parent, and use eulerAngles.z for the float rotation
            TowerProjectile proj = PoolManager.Instance.Spawn(
                data.projectilePrefab,
                null,
                origin,
                transform.eulerAngles.z
            );

            proj.Launch(this, target, origin);
        }
        else
        {
            // no projectile prefab: hit instantly
            if (IgnoresShield) target.TakeDamageIgnoringShield(ProjectileDamage);
            else target.TakeDamage(ProjectileDamage);
            OnHitEnemy(target);
        }
    }

    protected BaseEnemyData FindClosestEnemy()
    {
        BaseEnemyData bestTarget = null;
        float closestDist = float.MaxValue;

        foreach (BaseEnemyData enemy in BaseEnemyData.Active)
        {
            if (!enemy.IsTargetable(CanHitStealth)) continue;

            float dist = Vector2.Distance(transform.position, enemy.Body.position);

            if (InSight(enemy) && dist < closestDist)
            {
                closestDist = dist;
                bestTarget = enemy;
            }
        }
        return bestTarget;
    }
}
