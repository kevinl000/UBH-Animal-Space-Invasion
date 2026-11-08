using UnityEngine;

/// <summary>
/// Base for bosses: fly in and orbit (README "Orbiting Speed" is EnemyData.orbitSpeed), attack on a timer.
/// Healthy Goose (poop, single) and Healthy Skunk (skunk bombs, AoE) use this as-is;
/// the attack type comes from EnemyData.attackType.
/// </summary>
public class BossEnemy : BaseEnemyData
{
    protected override void OnInitialized()
    {
        Orbits = true;
    }
}
