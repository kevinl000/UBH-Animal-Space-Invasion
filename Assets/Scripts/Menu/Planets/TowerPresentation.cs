using UnityEngine;

// Display strings deliberately preserve README units; these are not combat or purchase data.
[CreateAssetMenu(menuName = "Planetary Defense/Tower Presentation")]
public class TowerPresentation : ScriptableObject
{
    public string displayName;
    public int unlockLevel;
    public Sprite portrait;
    public string health;
    public string cost;
    public string fuelRate;
    public string fieldOfView;
    public string damage;
    public string attackType;
    [TextArea] public string description;
}
