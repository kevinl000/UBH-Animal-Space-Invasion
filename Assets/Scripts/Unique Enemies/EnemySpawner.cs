using UnityEngine;
using EnemyAI;

/// <summary>
/// Spawns enemies chosen by the enemy AI (WaveDirectorBridge) and the halfway boss.
/// Setup: put this, a WaveDirectorBridge and an IPlanetDefense (e.g. SimplePlanetDefense) in the level scene,
/// assign the LevelData, and give each EnemyData a prefab.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public LevelData level;
    public WaveDirectorBridge bridge;
    [Tooltip("Any component that implements IPlanetDefense (SimplePlanetDefense works out of the box).")]
    public MonoBehaviour planetDefense;
    [Tooltip("Where enemies appear, in planet radii from the centre.")]
    public float spawnDistance = 4f;
    [Tooltip("Seconds between spawn attempts (one enemy at most per attempt).")]
    public float spawnInterval = 0.5f;
    public bool startOnAwake = true;

    public float Elapsed { get; private set; }
    public bool Running { get; private set; }

    private IPlanetDefense planet;
    private float nextSpawn;
    private bool timeUpRaised;

    private void Start()
    {
        if (startOnAwake) Begin();
    }

    public void Begin()
    {
        planet = planetDefense as IPlanetDefense;
        if (planet == null) { Debug.LogError("[EnemySpawner] planetDefense must implement IPlanetDefense."); return; }
        if (level == null) { Debug.LogError("[EnemySpawner] No LevelData assigned."); return; }

        level.Init();
        BaseEnemyData.Active.RemoveAll(e => e == null); // static list can outlive a play session in the editor
        if (bridge != null)
        {
            bridge.level = level;
            bridge.BeginLevel();
        }
        Elapsed = 0f;
        nextSpawn = 0f;
        timeUpRaised = false;
        Running = true;
    }

    public void Stop()
    {
        Running = false;
    }

    private void Update()
    {
        if (!Running) return;
        float previous = Elapsed;
        Elapsed += Time.deltaTime;

        if (bridge != null && bridge.ShouldShowBossWarning(previous, Elapsed))
            EnemyEvents.RaiseBossWarning(level.GetBossEnemyData());

        if (level.ShouldSpawnGuaranteedBoss(previous, Elapsed))
            SpawnBoss();

        if (level.maxTime > 0f && Elapsed >= level.maxTime)
        {
            if (!timeUpRaised) { timeUpRaised = true; EnemyEvents.RaiseLevelTimeUp(); }
            return; // no new normal enemies after the level time
        }

        if (Elapsed < nextSpawn || bridge == null) return;
        nextSpawn = Elapsed + spawnInterval;

        DefenseSnapshot snapshot = planet.BuildSnapshot();
        bridge.Tick(Elapsed, snapshot);
        LevelEnemyEntry entry = bridge.PickEntry(Elapsed); // null = the AI is still saving budget
        if (entry == null) return;

        SpawnModifiers mods = bridge.RollModifiers(entry, snapshot);
        Spawn(entry.enemy, mods.SpawnAngleDeg, mods.IsShiny, mods.Orbits, mods.Ticket, spawnDistance);
    }

    /// <summary>Creates one enemy. Returns null if the EnemyData has no prefab.</summary>
    public BaseEnemyData Spawn(EnemyData data, float angleDegrees, bool shiny, bool orbits, int ticket, float distance)
    {
        if (data == null) return null;
        if (data.prefab == null)
        {
            Debug.LogWarning("[EnemySpawner] EnemyData '" + data.name + "' has no prefab.");
            return null;
        }
        if (planet == null) planet = planetDefense as IPlanetDefense;

        BaseEnemyData enemy = Instantiate(data.prefab, planet.Root);
        enemy.name = data.name;
        enemy.Initialize(data, new EnemySpawnContext
        {
            Planet = planet,
            Spawner = this,
            Bridge = bridge,
            Ticket = ticket,
            AngleDegrees = angleDegrees,
            SpawnDistance = distance,
            IsShiny = shiny,
            Orbits = orbits,
        });
        return enemy;
    }

    /// <summary>For bosses summoning or releasing other enemies. Not tracked by the AI's learning.</summary>
    public BaseEnemyData SpawnMinion(EnemyData data, float angleDegrees, float distance)
    {
        bool shiny = Random.value < (data != null && data.isBoss ? 0.01f : 0.05f); // README shiny chances
        bool orbits = data != null && data.movement != EnemyMovement.StopAndShoot;
        return Spawn(data, angleDegrees, shiny, orbits, 0, distance);
    }

    private void SpawnBoss()
    {
        EnemyData boss = level.GetBossEnemyData();
        if (boss == null) return;
        float angle = Random.Range(0f, 360f);
        bool shiny = Random.value < 0.01f;
        if (bridge != null && bridge.Director != null)
        {
            SpawnModifiers mods = bridge.Director.RollModifiers(bridge.KindOf(boss), planet.BuildSnapshot());
            angle = mods.SpawnAngleDeg; // the AI aims the boss at the weakest side too
            shiny = mods.IsShiny;
        }
        BaseEnemyData b = Spawn(boss, angle, shiny, true, 0, spawnDistance);
        if (b != null) EnemyEvents.RaiseBossSpawned(b);
    }
}
