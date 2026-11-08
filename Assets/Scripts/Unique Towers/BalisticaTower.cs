using UnityEngine;

public class BallisticaTower : ShootingTower
{
    public float percentDamage = 0.20f; // 20%

    public override void OnHitEnemy(BaseEnemyData enemy)
    {
        // The projectile already dealt the flat base 'Damage' (which you can set to 0 in the ScriptableObject).
        // Here we add the massive 20% HP hit.
        float massiveHit = enemy.MaxHealth * percentDamage;

        if (IgnoresShield) enemy.TakeDamageIgnoringShield(massiveHit);
        else enemy.TakeDamage(massiveHit);
    }
}