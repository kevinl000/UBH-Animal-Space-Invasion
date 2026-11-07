using UnityEngine;

[CreateAssetMenu(menuName = "Planetary Defense/Planet Presentation")]
public class PlanetPresentation : ScriptableObject
{
    public string displayName;
    public string sceneName;
    public int level;
    public string landmark;
    [TextArea] public string description;
    public string bossName;
    public GameObject visualPrefab;

    public string LevelLabel => level == 0 ? "Tutorial" : "Level " + level;
}
