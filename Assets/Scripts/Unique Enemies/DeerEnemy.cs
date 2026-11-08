using UnityEngine;

/// <summary>
/// Basic Deer / Shielded Deer (README): fly towards the planet and orbit.
/// Periodically stop and charge the planet, hitting the slot below, then return to orbit.
/// </summary>
public class DeerEnemy : BaseEnemyData
{
    [Tooltip("Charge speed, planet radii per second.")]
    public float chargeSpeed = 3f;

    private enum State { Orbiting, ChargingIn, Returning }
    private State state = State.Orbiting;

    protected override void OnInitialized()
    {
        Orbits = true; // deer always orbit
    }

    protected override void Tick(float dt)
    {
        switch (state)
        {
            case State.Orbiting:
                Move(dt);
                if (!Arrived) return;
                attackTimer -= dt;
                if (attackTimer <= 0f) state = State.ChargingIn; // stop orbiting and charge
                break;

            case State.ChargingIn:
                Distance = Mathf.Max(1f, Distance - chargeSpeed * dt);
                if (Distance <= 1f)
                {
                    HitAt(AngleDegrees, damage, data.attackType == EnemyAttackType.AoE);
                    state = State.Returning;
                }
                break;

            case State.Returning:
                Distance = Mathf.Min(data.orbitDistance, Distance + chargeSpeed * dt);
                if (Distance >= data.orbitDistance)
                {
                    attackTimer = data.attackInterval;
                    state = State.Orbiting;
                }
                break;
        }
    }
}
