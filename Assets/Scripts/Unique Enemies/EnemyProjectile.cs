using System;
using UnityEngine;

/// <summary>
/// Visual projectile (poop, skunk bomb, horn). Flies from the enemy to the planet surface,
/// then calls back so the enemy deals its damage on impact. Optional: without a prefab, hits are instant.
/// Moves through its Rigidbody2D (kinematic) and comes from the level's PoolManager (it's a GameEntity).
/// </summary>
public class EnemyProjectile : GameEntity
{
    [Tooltip("Planet radii per second.")]
    public float speed = 2.5f;
    public PooledEffect impactEffect;
    public PooledEffect secondaryEffect;
    private Vector2 launchOffset;
    public void SetLaunchPosition(Vector2 position)
    {
        launchOffset = position - PositionNow();
        body.position = position;
    }

    private IPlanetDefense planet;
    private float startAngle, targetAngle, startDistance;
    private float distance;
    private Action onHit;
    private bool launched;

    public float Distance { get { return distance; } }

    protected override void OnDespawned()
    {
        launched = false;
        onHit = null;
    }

    /// <summary>Starts at the enemy (angle + distance in planet radii) and flies to the planet surface at targetAngle.</summary>
    public void Launch(IPlanetDefense planetDefense, float fromAngleDegrees, float fromDistance, float targetAngleDegrees, Action hit)
    {
        planet = planetDefense;
        launchOffset = Vector2.zero;
        onHit = hit;
        startAngle = fromAngleDegrees;
        targetAngle = targetAngleDegrees;
        startDistance = Mathf.Max(1.0001f, fromDistance);
        distance = startDistance;
        launched = true;
        // jump to the start point without sweeping from where the pooled instance was
        body.position = PositionNow();
        body.rotation = planet.Root.eulerAngles.z + targetAngleDegrees + 90f;
    }

    private Vector2 PositionNow()
    {
        // angle moves from the shooter to the target as the projectile falls
        float t = (startDistance - distance) / (startDistance - 1f);
        float delta = Mathf.DeltaAngle(startAngle, targetAngle);
        return (Vector2)planet.Root.TransformPoint(PlanetMath.ToLocal(startAngle + delta * t, distance, planet.Radius)) + launchOffset * (1f - t);
    }

    private void FixedUpdate()
    {
        if (!launched || !GameplayLoopController.CombatAllowed) return;
        distance = Mathf.Max(1f, distance - speed * Time.fixedDeltaTime);
        body.MovePosition(PositionNow());
        if (distance <= 1f)
        {
            launched = false;
            Action hit = onHit;
            onHit = null;
            CombatVisual.Play(impactEffect, PositionNow(), targetAngle - 90f);
            if (hit != null) hit();
            Despawn(); // back to the PoolManager
        }
    }
}
