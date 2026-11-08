using UnityEngine;

public class RadarTower : BaseTowerData
{
    public bool isGammaRadar = false;

    private float pulseTimer;

    protected override void OnInitialized() { pulseTimer = 0f; }

    // Radar's README attack type is N/A (None), and the base Tick skips None, so pulse on our own timer.
    protected override void Tick(float dt)
    {
        pulseTimer -= dt;
        if (pulseTimer > 0f) return;
        pulseTimer = fireRate > 0f ? fireRate : 0.5f;
        Fire();
    }

    protected override void Fire()
    {
        if (Visual != null) Visual.Attack();
        // FireRate acts as the pulse interval (e.g., pulse every 0.5s)
        for (int i = BaseEnemyData.Active.Count - 1; i >= 0; i--)
        {
            BaseEnemyData enemy = BaseEnemyData.Active[i];
            if (enemy.IsAlive && InSight(enemy))
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
