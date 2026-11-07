using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public int cost;
    public bool isBoss; // identity only: this type is a boss kind. Halfway spawn uses LevelEnemyEntry.isLevelBoss.
}
