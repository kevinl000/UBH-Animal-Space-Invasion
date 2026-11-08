using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// The only pooling script in a level. Put one on an empty GameObject in each level scene.
/// Enemies, towers and projectiles (anything that inherits GameEntity) are spawned and despawned through it:
///
///   BaseEnemyData goose = PoolManager.Instance.Spawn(gooseData.prefab, planetRoot);
///   BaseTowerData tower = PoolManager.Instance.Spawn(laserPrefab, slotTransform);
///   tower.Despawn();   // or PoolManager.Instance.Despawn(tower)
///
/// Inside, Unity's ObjectPool&lt;T&gt; can only build one prefab, so it keeps one ObjectPool per prefab in a dictionary.
/// Nobody else creates pools.
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Tooltip("Default capacity of each prefab's pool.")]
    public int defaultCapacity = 20;
    [Tooltip("Instances above this per prefab are destroyed instead of kept when they come back.")]
    public int maxSizePerPrefab = 200;

    private readonly Dictionary<GameEntity, ObjectPool<GameEntity>> pools = new Dictionary<GameEntity, ObjectPool<GameEntity>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PoolManager] There should be only one PoolManager in a level. Using the newest one.");
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        foreach (ObjectPool<GameEntity> p in pools.Values) p.Clear();
        pools.Clear();
        if (Instance == this) Instance = null;
    }

    /// <summary>Takes an instance of the prefab out of its pool (creating one if needed), parents it and activates it.</summary>
    public T Spawn<T>(T prefab, Transform parent) where T : GameEntity
    {
        if (prefab == null) { Debug.LogWarning("[PoolManager] Spawn called with no prefab."); return null; }
        GameEntity e = PoolFor(prefab).Get();
        e.transform.SetParent(parent, false);
        e.gameObject.SetActive(true);
        e.NotifySpawned();
        return (T)e;
    }

    /// <summary>Same as Spawn(prefab, parent), then places the Rigidbody2D (world position, degrees).</summary>
    public T Spawn<T>(T prefab, Transform parent, Vector2 position, float rotation) where T : GameEntity
    {
        T e = Spawn(prefab, parent);
        if (e != null && e.Body != null)
        {
            e.Body.position = position;
            e.Body.rotation = rotation;
        }
        return e;
    }

    /// <summary>Puts the entity back in its pool. Entities not made by the PoolManager are destroyed.</summary>
    public void Despawn(GameEntity e)
    {
        if (e == null) return;
        ObjectPool<GameEntity> pool;
        if (e.SourcePrefab == null || !pools.TryGetValue(e.SourcePrefab, out pool))
        {
            e.Despawn(); // falls back to Destroy
            return;
        }
        if (!e.IsSpawned) return; // already back in the pool
        pool.Release(e);
    }

    /// <summary>Creates instances ahead of time so the first spawns don't stutter.</summary>
    public void Prewarm(GameEntity prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        ObjectPool<GameEntity> pool = PoolFor(prefab);
        var temp = new List<GameEntity>();
        while (pool.CountAll < count) temp.Add(pool.Get());
        foreach (GameEntity e in temp) pool.Release(e);
    }

    /// <summary>How many instances of this prefab exist (in use + waiting in the pool).</summary>
    public int CountAll(GameEntity prefab)
    {
        ObjectPool<GameEntity> pool;
        return prefab != null && pools.TryGetValue(prefab, out pool) ? pool.CountAll : 0;
    }

    /// <summary>How many instances of this prefab are in use right now.</summary>
    public int CountActive(GameEntity prefab)
    {
        ObjectPool<GameEntity> pool;
        return prefab != null && pools.TryGetValue(prefab, out pool) ? pool.CountActive : 0;
    }

    private ObjectPool<GameEntity> PoolFor(GameEntity prefab)
    {
        ObjectPool<GameEntity> pool;
        if (pools.TryGetValue(prefab, out pool)) return pool;
        pool = new ObjectPool<GameEntity>(
            () =>
            {
                GameEntity e = Instantiate(prefab, transform);
                e.AssignPool(prefab);
                e.gameObject.SetActive(false);
                return e;
            },
            null, // activation happens in Spawn, after parenting
            e =>
            {
                if (e.IsSpawned) e.NotifyDespawned();
                e.gameObject.SetActive(false);
                e.transform.SetParent(transform, false);
            },
            e => { if (e != null) Destroy(e.gameObject); },
            true, defaultCapacity, maxSizePerPrefab);
        pools[prefab] = pool;
        return pool;
    }
}
