using UnityEngine;

public class RadarTower : BaseTowerData
{
    public bool isGammaRadar = false;

    protected override void Fire()
    {
        // FireRate acts as the pulse interval (e.g., pulse every 0.5s)
        foreach (BaseEnemyData enemy in BaseEnemyData.Active)
        {
            float dist = Vector2.Distance(transform.position, enemy.Body.position);

            if (dist <= data.fov)
            {
                // 1. Standard Radar: Reveal them 
                if (enemy.IsStealthed)
                {
                    enemy.RevealStealth();
                }

                // 2. Gamma Radar: Also deals damage to everyone in range
                if (isGammaRadar)
                {
                    if (IgnoresShield) enemy.TakeDamageIgnoringShield(Damage);
                    else enemy.TakeDamage(Damage);
                }
            }
        }
    }
}