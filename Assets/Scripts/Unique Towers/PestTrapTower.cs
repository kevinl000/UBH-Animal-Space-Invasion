using UnityEngine;

public class PestTrapTower : ShootingTower
{
    public float slowDuration = 3f;
    public float slowAmount = 0.5f; // 50% speed

    public override float ProjectileDamage { get { return 0f; } } // all its damage is the DoT below

    public override void OnHitEnemy(BaseEnemyData enemy)
    {
        enemy.ApplySlow(slowDuration, slowAmount);
        // README: 1 damage per second over 3 seconds, ignoring the shield (the projectile itself deals 0: ProjectileDamage)
        enemy.ApplyDamageOverTime(Damage, slowDuration);
    }
}