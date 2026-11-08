using System;
using UnityEngine;

public static class TowerEvents
{
    /// <summary>Fires when a player successfully builds a new tower.</summary>
    public static event Action<BaseTowerData, int> TowerBuilt;

    /// <summary>Fires when a player sells a tower (tower, refund amount).</summary>
    public static event Action<BaseTowerData, int> TowerSold;

    /// <summary>Fires if enemies are able to attack and destroy towers.</summary>
    public static event Action<BaseTowerData> TowerDestroyed;

    public static void RaiseTowerBuilt(BaseTowerData t, int cost)
    {
        if (TowerBuilt != null) TowerBuilt(t, cost);
    }
    public static void RaiseTowerSold(BaseTowerData t, int refund)
    {
        if (TowerSold != null) TowerSold(t, refund);
    }

    public static void RaiseTowerDestroyed(BaseTowerData t)
    {
        if (TowerDestroyed != null) TowerDestroyed(t);
    }
}
