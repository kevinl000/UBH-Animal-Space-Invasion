using UnityEngine;
using UnityEngine.SceneManagement;

// Owns the tutorial's timeline. Combat components consult the phase, not separate clocks.
[DefaultExecutionOrder(-100)]
public class GameplayLoopController : MonoBehaviour
{
    public enum GameplayPhase { Setup, Wave, Clear, Break, BossEntrance, BossFight, Victory, Defeat }

    public static GameplayLoopController Instance { get; private set; }
    public static bool CombatAllowed => Instance == null || Instance.Simulating;
    public LevelGameplayConfig config;
    public LevelManager levelManager;
    public LevelPlanetDefense defense;
    public EnemySpawner spawner;
    public TowerPlacementInput placement;
    public Camera battleCamera;
    public GameplayPhase Phase { get; private set; }
    public int WaveNumber { get; private set; }
    public float Remaining { get; private set; }
    public BaseEnemyData FinalBoss { get; private set; }
    public bool Ready => ready;
    public bool Paused => GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused;
    public bool Simulating => GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing
        && !SceneLoadingScreen.IsLoading && (Phase == GameplayPhase.Wave || Phase == GameplayPhase.Clear || Phase == GameplayPhase.BossFight);
    public bool CanBuild => ready && !Paused && !SceneLoadingScreen.IsLoading && (Phase == GameplayPhase.Setup || Phase == GameplayPhase.Break || Simulating);
    public bool CanSell => CanBuild && (Phase == GameplayPhase.Setup || Phase == GameplayPhase.Break);

    private float phaseElapsed;
    private float spawnElapsed;
    private int finalBossSpawnId;
    private bool bossKilled;
    private Vector3 homePosition;
    private float homeSize;
    private Vector3 entryStart;
    private bool ready;

    private void Awake()
    {
        Instance = this;
        Phase = GameplayPhase.Setup;
        spawner.externallyControlled = true;
        spawner.startOnAwake = false;
    }

    private void OnEnable() { EnemyEvents.Died += OnEnemyDied; }
    private void OnDisable() { EnemyEvents.Died -= OnEnemyDied; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Start()
    {
        if (!ValidConfiguration())
        {
            Debug.LogError("[Gameplay] Incomplete configuration. Start is disabled.", this);
            return;
        }
        GameManager.Instance.RegisterLoadedLevel(SceneManager.GetActiveScene().name);
        placement.towers = config.towers;
        placement.SelectTower(config.towers[0]);
        spawner.level = config.level;
        spawner.Begin();
        spawner.Stop();
        foreach (int slot in config.starterSlots)
        {
            string reason;
            if (!defense.TryPlaceTower(config.towers[0], slot, out reason, true)) Debug.LogError(reason, this);
        }
        ready = true;
    }

    private bool ValidConfiguration()
    {
        if (config == null || config.level == null || config.towers == null || config.towers.Length == 0 ||
            config.waves == null || config.waves.Length == 0 || config.finalBosses == null || config.finalBosses.Length == 0) return false;
        foreach (TowerData t in config.towers) if (t == null || t.prefab == null || t.projectilePrefab == null) return false;
        foreach (EnemyData b in config.finalBosses) if (b == null || b.prefab == null) return false;
        foreach (GameplayWave w in config.waves)
        {
            if (w == null || w.duration <= 0f || w.roster == null || w.roster.Length == 0) return false;
            foreach (LevelEnemyEntry e in w.roster) if (e == null || e.enemy == null || e.enemy.prefab == null) return false;
        }
        return true;
    }

    public void StartGame()
    {
        if (!ready || Phase != GameplayPhase.Setup || Paused || SceneLoadingScreen.IsLoading) return;
        BeginWave();
    }

    private void BeginWave()
    {
        WaveNumber++;
        GameplayWave wave = config.waves[WaveNumber - 1];
        SetPhase(GameplayPhase.Wave);
        Remaining = wave.duration;
        spawner.BeginWave(wave, spawnElapsed);
    }

    private void SetPhase(GameplayPhase phase)
    {
        Phase = phase;
        phaseElapsed = 0f;
        Remaining = 0f;
    }

    private void Update()
    {
        if (!ready || Paused || SceneLoadingScreen.IsLoading || !levelManager.IsAlive) return;
        float dt = Time.deltaTime;
        phaseElapsed += dt;
        switch (Phase)
        {
            case GameplayPhase.Wave:
                GameplayWave wave = config.waves[WaveNumber - 1];
                Remaining = Mathf.Max(0f, wave.duration - phaseElapsed);
                if (phaseElapsed >= wave.duration)
                {
                    spawnElapsed += wave.duration;
                    spawner.Stop();
                    if (WaveNumber == config.waves.Length) BeginBossEntrance();
                    else
                    {
                        spawner.SpawnEncounter(wave.miniboss);
                        SetPhase(GameplayPhase.Clear);
                    }
                }
                else spawner.TickWave(phaseElapsed);
                break;
            case GameplayPhase.Clear:
                if (BaseEnemyData.Active.Count == 0)
                {
                    PoolManager.Instance.ClearCombat();
                    float rest = config.waves[WaveNumber - 1].restDuration;
                    SetPhase(GameplayPhase.Break);
                    Remaining = rest;
                }
                break;
            case GameplayPhase.Break:
                Remaining = Mathf.Max(0f, config.waves[WaveNumber - 1].restDuration - phaseElapsed);
                if (Remaining <= 0f) BeginWave();
                break;
            case GameplayPhase.BossEntrance:
                TickBossEntrance();
                break;
        }
    }

    private void LateUpdate()
    {
        if (!ready || Phase == GameplayPhase.Victory || Phase == GameplayPhase.Defeat) return;
        // Resolve after physics and projectile impacts; a destroyed planet always loses.
        if (!levelManager.IsAlive) Finish(false);
        else if (bossKilled) Finish(true);
    }

    private void BeginBossEntrance()
    {
        SetPhase(GameplayPhase.BossEntrance);
        FinalBoss = spawner.SpawnEncounter(config.finalBosses[Random.Range(0, config.finalBosses.Length)]);
        if (FinalBoss == null) { Debug.LogError("[Gameplay] Final boss could not spawn.", this); return; }
        finalBossSpawnId = FinalBoss.SpawnId;
        homePosition = battleCamera.transform.position;
        homeSize = battleCamera.orthographicSize;
        entryStart = FinalBoss.Body.position;
    }

    private void TickBossEntrance()
    {
        if (FinalBoss == null) return;
        float t = Mathf.Clamp01(phaseElapsed / Mathf.Max(0.1f, config.bossEntranceDuration));
        float approach = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f) / 0.6f));
        Vector3 destination = defense.Root.TransformPoint(PlanetMath.ToLocal(FinalBoss.AngleDegrees, FinalBoss.data.orbitDistance, defense.Radius));
        FinalBoss.SetEntrancePosition(Vector3.Lerp(entryStart, destination, approach));
        float focus = t < 0.75f ? Mathf.SmoothStep(0f, 1f, t / 0.25f) : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.75f) / 0.25f);
        Vector3 close = FinalBoss.transform.position;
        close.z = homePosition.z;
        battleCamera.transform.position = Vector3.Lerp(homePosition, close, focus);
        battleCamera.orthographicSize = Mathf.Lerp(homeSize, homeSize * 0.42f, focus);
        if (t < 1f) return;
        battleCamera.transform.position = homePosition;
        battleCamera.orthographicSize = homeSize;
        FinalBoss.CompleteEntrance();
        SetPhase(GameplayPhase.BossFight);
    }

    private void OnEnemyDied(BaseEnemyData enemy)
    {
        if (enemy == FinalBoss && enemy.SpawnId == finalBossSpawnId) bossKilled = true;
    }

    private void Finish(bool won)
    {
        SetPhase(won ? GameplayPhase.Victory : GameplayPhase.Defeat);
        spawner.Stop();
        PoolManager.Instance.ClearCombat();
        if (won) GameManager.Instance.LevelComplete();
        else GameManager.Instance.GameOver();
    }
}
