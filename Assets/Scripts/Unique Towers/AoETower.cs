using UnityEngine;

public class AoETower : ShootingTower
{
    [Tooltip("Radius of the explosion/electricity arc")]
    public float effectRadius = 2f;

    public override void OnHitEnemy(BaseEnemyData primaryTarget)
    {
        // The primary target already took damage from the projectile hitting it.
        // Now we find everyone else in the blast radius.

        foreach (BaseEnemyData enemy in BaseEnemyData.Active)
        {
            // Skip the one we already hit, or dead/untargetable enemies
            if (enemy == primaryTarget || !enemy.IsTargetable(CanHitStealth)) continue;

            float dist = Vector2.Distance(primaryTarget.Body.position, enemy.Body.position);
            if (dist <= effectRadius)
            {
                // Apply the tower's damage to the surrounding enemies
                if (IgnoresShield) enemy.TakeDamageIgnoringShield(Damage);
                else enemy.TakeDamage(Damage);
            }
        }
    }
}