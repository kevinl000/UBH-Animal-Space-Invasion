using UnityEngine;

public class PestTrapTower : ShootingTower
{
    public float slowDuration = 3f;
    public float slowAmount = 0.5f; // 50% speed

    public override void OnHitEnemy(BaseEnemyData enemy)
    {
        enemy.ApplySlow(slowDuration, slowAmount);

        // Apply 1 tick of DOT immediately (further ticks would require a coroutine 
        // or passing a "Poisoned" state to the enemy script).
        enemy.TakeDamageIgnoringShield(Damage);
    }
}