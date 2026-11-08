using UnityEngine;

/// <summary>Mother Goose (README, level 1): no attack. Periodically calls in a flock of Basic Goose 3.</summary>
public class MotherGooseBoss : BossEnemy
{
    [Tooltip("EnemyData for the flock (README: Basic Goose 3).")]
    public EnemyData flockEnemy;
    [Tooltip("Geese per flock (not in README).")]
    public int flockSize = 4;
    [Tooltip("Seconds between flocks (not in README).")]
    public float flockInterval = 8f;
    [Tooltip("Angle spread of the flock around the boss, degrees.")]
    public float flockSpread = 30f;

    private float flockTimer;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        flockTimer = flockInterval * 0.5f;
    }

    protected override void Tick(float dt)
    {
        Move(dt);
        if (!Arrived) return;
        flockTimer -= dt;
        if (flockTimer > 0f) return;
        flockTimer = flockInterval;
        if (Visual != null) Visual.Attack();
        for (int i = 0; i < flockSize; i++)
        {
            float offset = flockSize > 1 ? Mathf.Lerp(-flockSpread / 2f, flockSpread / 2f, i / (float)(flockSize - 1)) : 0f;
            Summon(flockEnemy, offset);
        }
    }
}
