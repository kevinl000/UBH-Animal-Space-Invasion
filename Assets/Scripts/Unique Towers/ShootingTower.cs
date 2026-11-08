using UnityEngine;

public class ShootingTower : BaseTowerData
{
    protected override void Fire()
    {
        BaseEnemyData target = FindClosestEnemy();
        if (target == null) return; // Nobody in range

        if (data.projectilePrefab != null)
        {
            // FIX: Pass planet.Root as the parent, and use eulerAngles.z for the float rotation
            TowerProjectile proj = PoolManager.Instance.Spawn(
                data.projectilePrefab,
                null,
                transform.position,
                transform.eulerAngles.z
            );

            proj.Launch(this, target, transform.position);
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

            if (dist <= data.fov && dist < closestDist)
            {
                closestDist = dist;
                bestTarget = enemy;
            }
        }
        return bestTarget;
    }
}