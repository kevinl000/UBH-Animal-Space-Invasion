using System;

/// <summary>
/// Game-wide enemy events, so UI / economy code can react without referencing enemies directly.
/// </summary>
public static class EnemyEvents
{
    /// <summary>(money dropped, fuel dropped). Shiny drops are already doubled.</summary>
    public static event Action<int, int> Killed;
    public static event Action<BaseEnemyData> Spawned;
    /// <summary>Fires a few seconds before the halfway boss (for the "boss incoming" message).</summary>
    public static event Action<EnemyData> BossWarning;
    public static event Action<BaseEnemyData> BossSpawned;
    /// <summary>The level's maxTime has passed (no more normal spawns).</summary>
    public static event Action LevelTimeUp;

    public static void RaiseKilled(int money, int fuel) { if (Killed != null) Killed(money, fuel); }
    public static void RaiseSpawned(BaseEnemyData e) { if (Spawned != null) Spawned(e); }
    public static void RaiseBossWarning(EnemyData d) { if (BossWarning != null) BossWarning(d); }
    public static void RaiseBossSpawned(BaseEnemyData e) { if (BossSpawned != null) BossSpawned(e); }
    public static void RaiseLevelTimeUp() { if (LevelTimeUp != null) LevelTimeUp(); }
}
