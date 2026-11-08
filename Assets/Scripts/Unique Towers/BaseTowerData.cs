using UnityEngine;

public class BaseTowerData : GameEntity, IDamageable, IEnemyHitter, IFreezable
{
    public float maxHealth;
    public float cost;
    public float fuelIntake;
    public float fov;
    public float damage;
    public float fireRate;
    public bool ignoresShield;
    public bool canHitStealth;

    protected float currentHealth;

    public float Health { get { return currentHealth; } }
    public float MaxHealth { get { return maxHealth; } }
    public bool IsAlive { get { return !dead && currentHealth > 0f; } }

    public float Damage { get { return damage; } }

    public bool IgnoresShield { get { return ignoresShield; } }

    public bool CanHitStealth { get { return canHitStealth;  } }

    protected Rigidbody2D body;
    protected Collider2D hitCollider;

    private bool dead;

    public float ReceiveDamage(float amount)
    {
        currentHealth = currentHealth - amount;
        return amount;
    }

    public void OnHitEnemy(BaseEnemyData enemy)
    {
        throw new System.NotImplementedException();
    }

    public void Freeze(float seconds)
    {
        throw new System.NotImplementedException();
    }
}
