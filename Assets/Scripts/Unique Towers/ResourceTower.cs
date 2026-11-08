using UnityEngine;

public class ResourceTower : BaseTowerData
{
    public enum ResourceType { Money, Fuel }
    public ResourceType resourceType;
    public int generationAmount = 3;

    protected override void Fire()
    {
        // Instead of shooting, we "Fire" resources into the player's bank
        if (resourceType == ResourceType.Money)
        {
            TowerEvents.RaiseTowerMoneyTick(generationAmount);
        }
        else if (resourceType == ResourceType.Fuel)
        {
            TowerEvents.RaiseTowerMoneyTick(generationAmount);
        }
    }
}