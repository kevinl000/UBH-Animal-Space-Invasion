using UnityEngine;

/// <summary>Mother Skunk (README, level 2): periodically drops n skunk bombs (AoE, 3 damage each).</summary>
public class MotherSkunkBoss : BossEnemy
{
    [Tooltip("README 'n': bombs per volley (not decided yet).")]
    public int bombsPerVolley = 3;
    [Tooltip("Angle between bombs in one volley, degrees.")]
    public float bombSpacing = 25f;

    protected override void Attack()
    {
        for (int i = 0; i < bombsPerVolley; i++)
        {
            float offset = (i - (bombsPerVolley - 1) / 2f) * bombSpacing;
            FireAt(AngleDegrees + offset, damage, true);
        }
    }
}
