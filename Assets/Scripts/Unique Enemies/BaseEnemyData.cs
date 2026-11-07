using UnityEngine;

public class BaseEnemyData : MonoBehaviour
{
    public EnemyData data;
    public float maxHealth;
    public float speed;
    public float damage;

    protected float currentHealth;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    public virtual void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        currentHealth = maxHealth;

        // Copy combat stats from EnemyData once those fields exist on the type.
        // maxHealth = enemyData.maxHealth;
        // speed = enemyData.speed;
        // damage = enemyData.damage;
    }

    public virtual void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // Handle enemy death logic here (e.g., play animation, drop loot, etc.)
        // TODO: Implement death logic using Object Pooling for performance optimization
    }
}
