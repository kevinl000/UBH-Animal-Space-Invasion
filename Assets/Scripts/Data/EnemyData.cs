using UnityEngine;

public enum EnemyAttackType
{
    Single, // hits the tower (or planet) right below the enemy
    AoE,    // also hits the neighbouring slots
    None    // doesn't attack (e.g. Mother Goose, Animal Ship)
}

public enum EnemyMovement
{
    StopAndShoot,   // geese, skunks: fly in, stop at stopDistance, shoot
    OrbitAndCharge, // deer: fly in, orbit, periodically stop and charge the planet
    Orbit           // bosses: fly in and orbit while attacking
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public int cost;
    public bool isBoss; // identity only: this type is a boss kind. Halfway spawn uses LevelEnemyEntry.isLevelBoss.

    [Header("README stats")]
    public float maxHealth = 2f;
    public float damage = 1f;
    public EnemyAttackType attackType = EnemyAttackType.Single;
    public int moneyDrop = 1;
    public int fuelDrop = 1;
    [Tooltip("Negates the first incoming damage.")]
    public bool hasShield;
    [Tooltip("Invisible to towers that can't reveal stealth (only Radar / Gamma Radar reveal it).")]
    public bool isStealth;
    public EnemyMovement movement = EnemyMovement.StopAndShoot;
    [Tooltip("README 'Orbiting Speed' for bosses. Multiplies orbitDegreesPerSecond.")]
    public float orbitSpeed = 1f;

    [Header("Tuning (not in the README; distances are in planet radii)")]
    [Tooltip("Approach speed, planet radii per second.")]
    public float moveSpeed = 0.5f;
    [Tooltip("Where StopAndShoot enemies stop, measured from the planet centre.")]
    public float stopDistance = 1.8f;
    [Tooltip("Where orbiting enemies circle, measured from the planet centre.")]
    public float orbitDistance = 1.6f;
    public float orbitDegreesPerSecond = 15f;
    [Tooltip("Seconds between attacks (for deer: between charges).")]
    public float attackInterval = 1.5f;
    [Tooltip("AoE attacks hit every slot within this many degrees of the impact point.")]
    public float aoeSpreadDegrees = 30f;

    [Header("Prefabs")]
    [Tooltip("Prefab with a BaseEnemyData-derived component (BasicEnemy, DeerEnemy, or a boss).")]
    public BaseEnemyData prefab;
    [Tooltip("Optional. Without it, attacks hit instantly.")]
    public EnemyProjectile projectilePrefab;
}
