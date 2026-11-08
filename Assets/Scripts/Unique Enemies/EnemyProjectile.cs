using System;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Visual projectile (poop, skunk bomb, horn). Flies from the enemy to the planet surface,
/// then calls back so the enemy deals its damage on impact. Optional: without a prefab, hits are instant.
/// Moves through its Rigidbody2D (kinematic) and comes from the EnemySpawner's ObjectPool.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Tooltip("Planet radii per second.")]
    public float speed = 2.5f;

    private IPlanetDefense planet;
    private Rigidbody2D body;
    private IObjectPool<EnemyProjectile> pool;
    private float startAngle, targetAngle, startDistance;
    private float distance;
    private Action onHit;
    private bool launched;

    public float Distance { get { return distance; } }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>Called by the spawner when it creates the instance for its pool.</summary>
    public void SetPool(IObjectPool<EnemyProjectile> owner)
    {
        pool = owner;
    }

    /// <summary>Starts at the enemy (angle + distance in planet radii) and flies to the planet surface at targetAngle.</summary>
    public void Launch(IPlanetDefense planetDefense, float fromAngleDegrees, float fromDistance, float targetAngleDegrees, Action hit)
    {
        planet = planetDefense;
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
        return planet.Root.TransformPoint(PlanetMath.ToLocal(startAngle + delta * t, distance, planet.Radius));
    }

    private void FixedUpdate()
    {
        if (!launched) return;
        distance = Mathf.Max(1f, distance - speed * Time.fixedDeltaTime);
        body.MovePosition(PositionNow());
        if (distance <= 1f)
        {
            launched = false;
            if (onHit != null) onHit();
            onHit = null;
            if (pool != null) pool.Release(this);
            else Destroy(gameObject);
        }
    }
}
