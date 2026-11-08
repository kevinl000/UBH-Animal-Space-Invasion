using System;
using UnityEngine;

/// <summary>
/// Visual projectile (poop, skunk bomb, horn). Flies from the enemy to the planet surface,
/// then calls back so the enemy deals its damage on impact. Optional: without a prefab, hits are instant.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Tooltip("Planet radii per second.")]
    public float speed = 2.5f;

    private IPlanetDefense planet;
    private Vector3 target;
    private Action onHit;
    private bool launched;

    public void Launch(IPlanetDefense planetDefense, Vector3 fromLocal, float targetAngleDegrees, Action hit)
    {
        planet = planetDefense;
        onHit = hit;
        transform.localPosition = fromLocal;
        target = PlanetMath.ToLocal(targetAngleDegrees, 1f, planet.Radius);
        launched = true;
    }

    private void Update()
    {
        if (!launched) return;
        float step = speed * planet.Radius * Time.deltaTime;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, step);
        if ((transform.localPosition - target).sqrMagnitude <= 0.0001f * planet.Radius * planet.Radius)
        {
            launched = false;
            if (onHit != null) onHit();
            Destroy(gameObject);
        }
    }
}
