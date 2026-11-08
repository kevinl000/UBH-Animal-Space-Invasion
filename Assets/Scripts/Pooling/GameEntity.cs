using UnityEngine;

/// <summary>
/// Base class for everything that lives in a level and is reused through the PoolManager:
/// enemies (BaseEnemyData), towers (BaseTowerData), projectiles.
///
///   GameEntity
///   ├── BaseEnemyData ── BasicEnemy (geese, skunks), DeerEnemy, bosses
///   ├── BaseTowerData ── (towers)
///   └── EnemyProjectile
///
/// Every entity has a Rigidbody2D (added automatically) and should have a Collider2D.
/// Never Destroy an entity: call Despawn() and it goes back to the PoolManager.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class GameEntity : MonoBehaviour
{
    /// <summary>The prefab this instance was made from (its pool key). Null if it wasn't spawned by the PoolManager.</summary>
    public GameEntity SourcePrefab { get; private set; }
    /// <summary>True between Spawn and Despawn.</summary>
    public bool IsSpawned { get; private set; }
    /// <summary>Changes every time the instance is reused, so an old reference can tell it's a different object now.</summary>
    public int SpawnId { get; private set; }
    public Rigidbody2D Body { get { return body; } }
    public Collider2D Collider { get { return hitCollider; } }

    protected Rigidbody2D body;
    protected Collider2D hitCollider;
    protected Animator[] spriteAnimators;
    private bool destroyedWithoutPool;
    private static int nextSpawnId;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>(); // prefab made before RequireComponent was added
        hitCollider = GetComponent<Collider2D>();
        spriteAnimators = GetComponentsInChildren<Animator>(true);
        ConfigureBody(body);
    }

    /// <summary>Default: kinematic (moved by code with MovePosition), no gravity, interpolated. Override for other setups.</summary>
    protected virtual void ConfigureBody(Rigidbody2D rb)
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true; // also report contacts with kinematic / static colliders
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>Called by the PoolManager when the instance is created for its pool. Don't call yourself.</summary>
    public void AssignPool(GameEntity prefab)
    {
        SourcePrefab = prefab;
    }

    /// <summary>Called by the PoolManager right after the instance is taken out and activated. Don't call yourself.</summary>
    public void NotifySpawned()
    {
        IsSpawned = true;
        destroyedWithoutPool = false;
        SpawnId = ++nextSpawnId;
        foreach (Animator animator in spriteAnimators)
        {
            animator.speed = 1f;
            animator.Rebind();
            animator.Update(0f);
        }
        OnSpawned();
    }

    /// <summary>Called by the PoolManager just before the instance is deactivated and put back. Don't call yourself.</summary>
    public void NotifyDespawned()
    {
        IsSpawned = false;
        OnDespawned();
    }

    /// <summary>Reset per-use state here (HP, timers...). The object is active.</summary>
    protected virtual void OnSpawned() { }

    protected virtual void LateUpdate()
    {
        // Towers/enemies bind their own power/status visuals through CombatVisual.
        if (this is BaseTowerData || this is BaseEnemyData) return;
        foreach (Animator animator in spriteAnimators)
            animator.speed = GameplayLoopController.CombatAllowed ? 1f : 0f;
    }

    /// <summary>Clean up here (unregister from lists...). The object is still active.</summary>
    protected virtual void OnDespawned() { }

    /// <summary>Removes the entity from the level: back to its pool, or destroyed if it wasn't pooled. Safe to call twice.</summary>
    public void Despawn()
    {
        if (SourcePrefab != null && PoolManager.Instance != null)
        {
            if (IsSpawned) PoolManager.Instance.Despawn(this);
            return;
        }
        // placed by hand in the scene or no PoolManager: fall back to Destroy
        if (destroyedWithoutPool) return;
        destroyedWithoutPool = true;
        IsSpawned = false;
        OnDespawned();
        Destroy(gameObject);
    }
}
