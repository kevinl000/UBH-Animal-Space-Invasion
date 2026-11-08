using UnityEngine;

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
    public int fuelIntake;
    public float fireRate;
    public float fov;
    public TowerAttackType attackType = TowerAttackType.Single;

    [Header("Prefabs")]
    [Tooltip("Prefab with a BaseTowerData-derived component.")]
    public BaseTowerData prefab;
    [Tooltip("Optional. Without it, attacks hit instantly.")]
    public TowerProjectile projectilePrefab;
}
