using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Davis Nova (README): towers have a lifespan and explode.
/// Every tower built in this level lives `lifespanSeconds`. In the last `warningSeconds` it flashes red;
/// then it explodes, damaging enemies within `explosionRadius`, and is destroyed (TowerEvents.TowerDestroyed).
/// Put this next to LevelPlanetDefense only in levels that use the rule.
/// </summary>
public class TowerLifespan : MonoBehaviour
{
    [Tooltip("Seconds a tower lives after it's built.")]
    public float lifespanSeconds = 40f;
    [Tooltip("It flashes red for this long before exploding.")]
    public float warningSeconds = 5f;
    [Tooltip("World units around the tower.")]
    public float explosionRadius = 1.2f;
    public float explosionDamage = 3f;
    [Tooltip("Optional explosion visual (a PooledEffect prefab).")]
    public PooledEffect explosionEffect;
    public Color warningColor = new Color(1f, 0.3f, 0.2f);

    /// <summary>(tower, enemies hit). For sounds / UI.</summary>
    public static event System.Action<BaseTowerData, int> Exploded;

    private class Life
    {
        public BaseTowerData Tower;
        public int SpawnId;
        public float Age;
        public SpriteRenderer[] Renderers;
        public Color[] Colors;
    }

    private readonly List<Life> lives = new List<Life>();

    /// <summary>Seconds left for this tower, or -1 if it isn't tracked.</summary>
    public float TimeLeft(BaseTowerData tower)
    {
        foreach (Life l in lives)
            if (l.Tower == tower && l.SpawnId == tower.SpawnId) return Mathf.Max(0f, lifespanSeconds - l.Age);
        return -1f;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // start tracking newly built towers
        foreach (BaseTowerData t in BaseTowerData.Active)
        {
            if (!t.IsAlive) continue;
            bool known = false;
            foreach (Life l in lives) if (l.Tower == t && l.SpawnId == t.SpawnId) { known = true; break; }
            if (!known) lives.Add(new Life { Tower = t, SpawnId = t.SpawnId });
        }

        for (int i = lives.Count - 1; i >= 0; i--)
        {
            Life l = lives[i];
            // sold / destroyed / reused from the pool: forget it
            if (l.Tower == null || !l.Tower.IsAlive || l.Tower.SpawnId != l.SpawnId)
            {
                RestoreColors(l);
                lives.RemoveAt(i);
                continue;
            }
            if (l.Tower.IsDisabled) continue; // no fuel: the clock stops too

            l.Age += dt;
            float left = lifespanSeconds - l.Age;
            if (left <= 0f)
            {
                RestoreColors(l);
                lives.RemoveAt(i);
                Explode(l.Tower);
            }
            else if (left <= warningSeconds)
            {
                Flash(l, left);
            }
        }
    }

    private void Explode(BaseTowerData tower)
    {
        Vector2 at = tower.Body != null ? tower.Body.position : (Vector2)tower.transform.position;
        int hit = 0;
        foreach (BaseEnemyData e in BaseEnemyData.Active.ToArray())
        {
            if (e == null || !e.IsAlive || e.Body == null) continue;
            if (Vector2.Distance(at, e.Body.position) > explosionRadius) continue;
            e.TakeDamage(explosionDamage);
            hit++;
        }
        if (explosionEffect != null && PoolManager.Instance != null)
            PoolManager.Instance.Spawn(explosionEffect, transform, at, 0f);
        if (Exploded != null) Exploded(tower, hit);
        tower.ReceiveDamage(tower.Health + 1f); // destroys it: TowerEvents.TowerDestroyed, back to the pool
    }

    private void Flash(Life l, float left)
    {
        if (l.Renderers == null)
        {
            l.Renderers = l.Tower.GetComponentsInChildren<SpriteRenderer>();
            l.Colors = new Color[l.Renderers.Length];
            for (int i = 0; i < l.Renderers.Length; i++) l.Colors[i] = l.Renderers[i].color;
        }
        // faster blinking as it gets closer
        float speed = Mathf.Lerp(12f, 4f, left / Mathf.Max(0.01f, warningSeconds));
        float t = (Mathf.Sin(l.Age * speed) + 1f) * 0.5f;
        for (int i = 0; i < l.Renderers.Length; i++)
            if (l.Renderers[i] != null) l.Renderers[i].color = Color.Lerp(l.Colors[i], warningColor, t);
    }

    private static void RestoreColors(Life l)
    {
        if (l.Renderers == null) return;
        for (int i = 0; i < l.Renderers.Length; i++)
            if (l.Renderers[i] != null) l.Renderers[i].color = l.Colors[i];
        l.Renderers = null;
    }
}
