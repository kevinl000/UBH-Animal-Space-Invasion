using UnityEngine;

/// <summary>
/// Sneaky Healthy Goose (README, level 4): every n seconds goes into stealth mode and heals HP/s.
/// While in stealth it doesn't attack. Otherwise poops periodically (single, 2 damage).
/// </summary>
public class SneakyHealthyGooseBoss : BossEnemy
{
    [Tooltip("README 'n': seconds between stealth phases (not decided yet).")]
    public float secondsBetweenStealth = 10f;
    [Tooltip("How long each stealth phase lasts (not in README).")]
    public float stealthDuration = 4f;
    [Tooltip("README 'HP/s' healed while in stealth (not decided yet).")]
    public float healPerSecond = 2f;

    private float phaseTimer;
    private bool inStealth;

    public bool InStealthMode { get { return inStealth; } }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        phaseTimer = secondsBetweenStealth;
        inStealth = false; // pooled instances start fresh
    }

    protected override void Tick(float dt)
    {
        Move(dt);
        if (!Arrived) return;

        phaseTimer -= dt;
        if (phaseTimer <= 0f)
        {
            inStealth = !inStealth;
            IsStealthed = inStealth || (data != null && data.isStealth);
            phaseTimer = inStealth ? stealthDuration : secondsBetweenStealth;
        }

        if (inStealth)
        {
            Heal(healPerSecond * dt); // no attacks while in stealth
        }
        else
        {
            TickAttack(dt);
        }
    }
}
