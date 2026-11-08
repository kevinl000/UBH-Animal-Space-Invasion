using UnityEngine;

/// <summary>Restraining Buck (README, level 3): shoots horns (single, 5 damage) that freeze the closest tower for n seconds.</summary>
public class RestrainingBuckBoss : BossEnemy
{
    [Tooltip("README 'n': freeze duration in seconds (not decided yet).")]
    public float freezeSeconds = 3f;

    protected override void Attack()
    {
        FireAt(AngleDegrees, damage, false);
        if (planet != null) planet.FreezeNearestTower(AngleDegrees, freezeSeconds);
    }
}
