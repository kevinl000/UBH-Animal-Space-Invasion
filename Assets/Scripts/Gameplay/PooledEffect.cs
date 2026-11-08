using UnityEngine;

/// <summary>
/// A one-shot visual effect (explosion, puff) that comes from the PoolManager and goes back by itself after `lifetime`.
/// Usage: PoolManager.Instance.Spawn(effectPrefab, parent, worldPosition, 0f);
/// </summary>
public class PooledEffect : GameEntity
{
    [Tooltip("Seconds before it returns to the pool (match the animation length).")]
    public float lifetime = 0.6f;

    private float timer;

    protected override void OnSpawned()
    {
        timer = lifetime;
    }

    private void FixedUpdate()
    {
        if (!IsSpawned) return;
        timer -= Time.fixedDeltaTime;
        if (timer <= 0f) Despawn();
    }
}
