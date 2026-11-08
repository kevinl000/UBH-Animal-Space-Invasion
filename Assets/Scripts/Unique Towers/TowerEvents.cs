using System;
using UnityEngine;

public static class TowerEvents
{
    /// <summary>Fires when a player successfully builds a new tower.</summary>
    public static event Action<int> TowerBuilt;

    /// <summary>Fires when a player sells a tower (tower, refund amount).</summary>
    public static event Action<int> TowerSold;

    /// <summary>Fires if enemies are able to attack and destroy towers.</summary>
    public static event Action<BaseTowerData> TowerDestroyed;

    public static event Action<int> TowerFuelTick;
    public static event Action<int> TowerMoneyTick;

    public static void RaiseTowerBuilt(int cost)
    {
        if (TowerBuilt != null) TowerBuilt(cost);
    }
    public static void RaiseTowerSold(int refund)
    {
        if (TowerSold != null) TowerSold(refund);
    }

    public static void RaiseTowerDestroyed(BaseTowerData t)
    {
        if (TowerDestroyed != null) TowerDestroyed(t);
    }

    public static void RaiseTowerFuelTick(int amount)
    {
        if (TowerFuelTick != null) TowerFuelTick(amount);
    }

    public static void RaiseTowerMoneyTick(int amount)
    {
        if (TowerMoneyTick != null) TowerMoneyTick(amount);
    }
}
