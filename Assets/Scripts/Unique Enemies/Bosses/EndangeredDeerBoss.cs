using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Endangered Deer (README, level 6): spawns a Shielded Deer every n seconds.
/// Can't take damage while any of its Shielded Deer is alive. Shoots horns (single, 5 damage).
/// </summary>
public class EndangeredDeerBoss : BossEnemy
{
    [Tooltip("EnemyData for the guards (README: Shielded Deer).")]
    public EnemyData guardEnemy;
    [Tooltip("README 'n': seconds between guards (not decided yet).")]
    public float guardInterval = 12f;

    private readonly List<BaseEnemyData> guards = new List<BaseEnemyData>();
    private float guardTimer;

    public int LivingGuards { get { guards.RemoveAll(g => g == null || !g.IsAlive); return guards.Count; } }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        guardTimer = 0f; // first guard as soon as it arrives
    }

    protected override void Tick(float dt)
    {
        base.Tick(dt);
        if (Arrived)
        {
            guardTimer -= dt;
            if (guardTimer <= 0f)
            {
                guardTimer = guardInterval;
                BaseEnemyData g = Summon(guardEnemy, 0f);
                if (g != null) guards.Add(g);
            }
        }
        IsInvulnerable = LivingGuards > 0;
    }
}
