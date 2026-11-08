using UnityEngine;
using EnemyAI;

public enum TowerAttackType
{
    Single, // single shot
    AoE,    // also hits the neighbouring enemies
    Dot,    // damage over time
    None    // doesn't attack (e.g. radar)
}

[CreateAssetMenu(fileName = "TowerData", menuName = "Scriptable Objects/TowerData")]
public class TowerData : ScriptableObject
{
    [Header("README stats")]
    public float maxHealth = 1f;
    public float damage = 1f;
    public int cost;
    [Tooltip("Negative = fuel cost per shot (Gamma Radar: per damaging pulse). Positive = fuel generated per second. Zero = free.")]
    public int fuelIntake;
    public float fireRate;
    [Tooltip("Field of view in degrees (README), centred on the tower's outward direction. 0 or 360+ = all around.")]
    public float fov;
    public TowerAttackType attackType = TowerAttackType.Single;

    [Header("Not in the README")]
    [Tooltip("Targeting range in world units. 0 = unlimited (only the FOV limits it).")]
    public float range;
    [Tooltip("What the enemy AI sees this tower as (it aims at gaps in tower coverage).")]
    public TowerKind kind;

    [Header("Abilities")]
    public bool ignoresShield;
    public bool canHitStealth;

    [Header("Prefabs")]
    [Tooltip("Prefab with a BaseTowerData-derived component.")]
    public BaseTowerData prefab;
    [Tooltip("Optional. Without it, attacks hit instantly.")]
    public TowerProjectile projectilePrefab; // Assuming you have a TowerProjectile class
}
