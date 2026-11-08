using UnityEngine;
using EnemyAI;

/// <summary>
/// Spawns enemies chosen by the enemy AI (WaveDirectorBridge) and the halfway boss.
/// Setup: put this, a WaveDirectorBridge and an IPlanetDefense (e.g. SimplePlanetDefense) in the level scene,
/// assign the LevelData, and give each EnemyData a prefab.
/// It only decides what to spawn and when; the instances come from the level's PoolManager.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public LevelData level;
    public WaveDirectorBridge bridge;
    [Tooltip("Any component that implements IPlanetDefense (SimplePlanetDefense works out of the box).")]
    public MonoBehaviour planetDefense;
    [Tooltip("Fallback spawn distance in planet radii when no gameplay zoom view is available.")]
    public float spawnDistance = 4f;
    [Tooltip("Seconds between spawn attempts (one enemy at most per attempt).")]
    public float spawnInterval = 0.5f;
    public bool startOnAwake = true;
    public bool externallyControlled;
    private GameplayWave currentWave;
    private float waveStart;
    private float nextWaveSpawn;
    private float reinforcementCredit;

    [Tooltip("The level's PoolManager. Empty = PoolManager.Instance.")]
    public PoolManager pool;
    [Tooltip("Instances the PoolManager creates up front per enemy in the level when Begin() runs, so the first wave doesn't stutter.")]
    public int prewarmPerEnemy = 5;

    public float Elapsed { get; private set; }
    public bool Running { get; private set; }

    private IPlanetDefense planet;
    private float nextSpawn;
    private bool timeUpRaised;
    private PlanetPageZoom spawnView;

    private PoolManager Pool { get { return pool != null ? pool : PoolManager.Instance; } }

    private void Start()
    {
        if (startOnAwake) Begin();
    }

    public void Begin()
    {
        planet = planetDefense as IPlanetDefense;
        if (planet == null) { Debug.LogError("[EnemySpawner] planetDefense must implement IPlanetDefense."); return; }
        if (level == null) { Debug.LogError("[EnemySpawner] No LevelData assigned."); return; }
        if (Pool == null) { Debug.LogError("[EnemySpawner] No PoolManager in the scene. Add one (there should be exactly one per level)."); return; }
        spawnView = FindAnyObjectByType<PlanetPageZoom>();

        if (!externallyControlled) level.Init();
        BaseEnemyData.Active.RemoveAll(e => e == null); // static list can outlive a play session in the editor
        if (bridge != null)
        {
            bridge.level = level;
            bridge.BeginLevel();
        }
        Prewarm();
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
        if (!Running || externallyControlled || !GameplayLoopController.CombatAllowed) return;
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
    public BaseEnemyData Spawn(EnemyData data, float angleDegrees, bool shiny, bool orbits, int ticket, float distance, bool atViewBorder = true)
    {
        if (data == null) return null;
        if (data.prefab == null)
        {
            Debug.LogWarning("[EnemySpawner] EnemyData '" + data.name + "' has no prefab.");
            return null;
        }
        if (planet == null) planet = planetDefense as IPlanetDefense;

        if (Pool == null) { Debug.LogError("[EnemySpawner] No PoolManager in the scene."); return null; }

        BaseEnemyData enemy = Pool.Spawn(data.prefab, planet.Root);
        enemy.name = data.name;
        enemy.Initialize(data, new EnemySpawnContext
        {
            Planet = planet,
            Spawner = this,
            Bridge = bridge,
            Ticket = ticket,
            AngleDegrees = angleDegrees,
            SpawnDistance = distance,
            SpawnAtViewBorder = atViewBorder,
            IsShiny = shiny,
            Orbits = orbits,
        });
        return enemy;
    }

    public float GetEntryDistance(float angleDegrees, float clearance, float fallback)
    {
        float distance;
        return spawnView != null && spawnView.TryGetSpawnDistance(planet.Root, planet.Radius, angleDegrees, clearance, out distance)
            ? distance : fallback;
    }

    /// <summary>For bosses summoning or releasing other enemies. Not tracked by the AI's learning.</summary>
    public BaseEnemyData SpawnMinion(EnemyData data, float angleDegrees, float distance)
    {
        bool shiny = Random.value < (data != null && data.isBoss ? 0.01f : 0.05f); // README shiny chances
        bool orbits = data != null && data.movement != EnemyMovement.StopAndShoot;
        return Spawn(data, angleDegrees, shiny, orbits, 0, distance, false);
    }

    public void BeginWave(GameplayWave wave, float completedSpawnTime)
    {
        currentWave = wave;
        waveStart = completedSpawnTime;
        nextWaveSpawn = 0f;
        bridge.BeginWave(wave);
        Running = true;
    }

    public void TickWave(float elapsed)
    {
        if (!Running || currentWave == null || !GameplayLoopController.CombatAllowed) return;
        Elapsed = waveStart + elapsed;
        if (elapsed < nextWaveSpawn) return;
        nextWaveSpawn = elapsed + GameplayLoopController.Instance.config.spawnInterval;
        DefenseSnapshot snapshot = planet.BuildSnapshot();
        bridge.Tick(Elapsed, snapshot);
        float available = currentWave.costBudget * Mathf.Clamp01(elapsed / currentWave.duration);
        LevelEnemyEntry entry = bridge.PickWaveEntry(available);
        if (entry == null) return;
        SpawnModifiers mods = bridge.RollModifiers(entry, snapshot);
        BaseEnemyData enemy = Spawn(entry.enemy, mods.SpawnAngleDeg, mods.IsShiny, mods.Orbits, mods.Ticket, spawnDistance);
        if (enemy != null) bridge.RecordWaveSpawn(entry);
        else bridge.ReportGone(mods.Ticket);
    }

    public void BeginBossReinforcements(float completedSpawnTime)
    {
        waveStart = completedSpawnTime;
        nextWaveSpawn = 0f;
        reinforcementCredit = 0f;
        // Reset only the spending window; retain the roster, director and its learning.
        bridge.BeginWave(currentWave, GameplayLoopController.Instance.config.bossReinforcementBudgetFraction);
        Running = true;
    }

    public void TickBossReinforcements(float elapsed, float dt, float bossHealthFraction)
    {
        if (!Running || currentWave == null || !GameplayLoopController.CombatAllowed) return;
        LevelGameplayConfig config = GameplayLoopController.Instance.config;
        Elapsed = waveStart + elapsed;
        float pressure = Mathf.InverseLerp(config.bossReinforcementStopHealth,
            config.bossReinforcementTaperHealth, bossHealthFraction);
        int alive = 0;
        foreach (BaseEnemyData enemy in BaseEnemyData.Active)
            if (enemy.IsAlive && enemy.data != null && !enemy.data.isBoss) alive++;
        if (pressure <= 0f || alive >= config.bossReinforcementMaxAlive || bridge.WaveRemainingBudget <= 0f)
        {
            // Crowding or low boss HP must never bank a catch-up burst.
            reinforcementCredit = 0f;
            nextWaveSpawn = elapsed + config.spawnInterval;
            return;
        }
        float rate = currentWave.costBudget / currentWave.duration * config.bossReinforcementRate;
        reinforcementCredit = Mathf.Min(bridge.WaveRemainingBudget, reinforcementCredit + rate * pressure * dt);
        if (elapsed < nextWaveSpawn) return;
        nextWaveSpawn = elapsed + config.spawnInterval;
        DefenseSnapshot snapshot = planet.BuildSnapshot();
        bridge.Tick(Elapsed, snapshot);
        LevelEnemyEntry entry = bridge.PickWaveEntry(bridge.WaveSpent + reinforcementCredit);
        if (entry == null) return;
        SpawnModifiers mods = bridge.RollModifiers(entry, snapshot);
        BaseEnemyData spawned = Spawn(entry.enemy, mods.SpawnAngleDeg, mods.IsShiny, mods.Orbits, mods.Ticket, spawnDistance);
        if (spawned != null)
        {
            reinforcementCredit = Mathf.Max(0f, reinforcementCredit - LevelData.EffectiveCost(entry.enemy));
            bridge.RecordWaveSpawn(entry);
        }
        else bridge.ReportGone(mods.Ticket);
    }

    public BaseEnemyData SpawnEncounter(EnemyData data)
    {
        if (data == null) { Debug.LogError("[EnemySpawner] Missing encounter data."); return null; }
        bool shiny = Random.value < (data.isBoss ? 0.01f : 0.05f);
        bool orbit = data.isBoss || Random.value < 0.5f;
        BaseEnemyData enemy = Spawn(data, Random.Range(0f, 360f), shiny, orbit, 0, spawnDistance);
        if (data.isBoss && enemy != null) EnemyEvents.RaiseBossSpawned(enemy);
        return enemy;
    }

    /// <summary>Asks the PoolManager to create a few instances of every enemy in the level up front.</summary>
    private void Prewarm()
    {
        if (prewarmPerEnemy <= 0 || level == null || level.enemyDataList == null) return;
        foreach (LevelEnemyEntry entry in level.enemyDataList)
            if (entry != null && entry.enemy != null && entry.enemy.prefab != null)
                Pool.Prewarm(entry.enemy.prefab, prewarmPerEnemy);
    }

    /// <summary>Sends every living enemy back to the PoolManager (no drops), e.g. when the level ends.</summary>
    public void DespawnAll()
    {
        foreach (BaseEnemyData e in BaseEnemyData.Active.ToArray())
            if (e != null) e.Despawn();
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
