using UnityEngine;

public class ResourceTower : BaseTowerData
{
    public enum ResourceType { Money, Fuel }
    public ResourceType resourceType;
    public int generationAmount = 3;

    private float generateTimer;

    protected override void OnInitialized() { generateTimer = fireRate > 0f ? fireRate : 1f; }

    // Generates on its own timer even when attackType is None (the base Tick skips None).
    protected override void Tick(float dt)
    {
        if (generationAmount == 0) return; // Oil Rig: its +3 F/s already comes from fuelIntake
        generateTimer -= dt;
        if (generateTimer > 0f) return;
        generateTimer = fireRate > 0f ? fireRate : 1f; // README: per second
        Fire();
    }

    protected override void Fire()
    {
        // Instead of shooting, we "Fire" resources into the player's bank
        if (resourceType == ResourceType.Money)
        {
            TowerEvents.RaiseTowerMoneyTick(generationAmount);
        }
        else if (resourceType == ResourceType.Fuel)
        {
            TowerEvents.RaiseTowerFuelTick(generationAmount);
        }
    }
}
