using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour, IDamageable
{
    [Header("Level Configuration")]
    [SerializeField] private LevelData levelData; // Assign your ScriptableObject here in the Inspector

    public LevelData Level { get { return levelData; } }

    [Header("UI")]
    public Text hp;
    public Text money;
    public Text fuel;
    public Text time;
    public Text wave;

    public float PlanetHP { get { return currentHealth; } }
    public int TotalMoney { get; private set; }
    public float TotalFuel { get; private set; }

    private float currentHealth;
    private bool dead;

    public bool IsAlive { get { return !dead && currentHealth > 0f; } }

    public Rigidbody2D planetBody;

    private void Awake()
    {
        // keep the planet body assigned in the Inspector (the Level Manager object itself has no Rigidbody2D)
        if (planetBody == null) planetBody = GetComponent<Rigidbody2D>();

        if (levelData != null)
        {
            // Reset the ScriptableObject's runtime values (SpentBudget, etc.) for a fresh run
            levelData.Init();

            // Pull starting values from the assigned LevelData
            currentHealth = Mathf.Max(0f, levelData.planetMaxHealth);
            TotalMoney = Mathf.Max(0, levelData.money);
            TotalFuel = Mathf.Max(0f, levelData.fuel);

            // Apply physical properties
            if (planetBody != null) planetBody.gravityScale = levelData.planetGravity;
            else Debug.LogWarning("[LevelManager] No planetBody: assign the planet's Rigidbody2D so it can rotate.");
        }
        else
        {
            Debug.LogError("[LevelManager] No LevelData assigned in the inspector!");
        }

        RefreshResourceBar();
    }

    // ---------------------------------------------------------------- Events

    private void OnEnable()
    {
        EnemyEvents.Killed += ChangeResources;
        TowerEvents.TowerBuilt += PayForTower;
        TowerEvents.TowerSold += ChangeMoney;
        TowerEvents.TowerFuelTick += ChangeFuel;
        TowerEvents.TowerMoneyTick += ChangeMoney; 
    }

    private void OnDisable()
    {
        EnemyEvents.Killed -= ChangeResources;
        TowerEvents.TowerBuilt -= PayForTower;
        TowerEvents.TowerSold -= ChangeMoney;
        TowerEvents.TowerFuelTick -= ChangeFuel;
        TowerEvents.TowerMoneyTick -= ChangeMoney;
    }


    // ---------------------------------------------------------------- Core Loop

    private void FixedUpdate()
    {
        if (currentHealth <= 0f || levelData == null || planetBody == null) return;
        var loop = GameplayLoopController.Instance;
        if (loop != null && (loop.Paused || loop.Phase == GameplayLoopController.GameplayPhase.BossEntrance ||
            loop.Phase == GameplayLoopController.GameplayPhase.Victory || loop.Phase == GameplayLoopController.GameplayPhase.Defeat)) return;

        // Use rotation speed and direction directly from LevelData
        float direction = levelData.isClockwise ? -1f : 1f;
        planetBody.MoveRotation(planetBody.rotation + direction * levelData.planetRotationSpeed * Time.fixedDeltaTime);
    }

    // ---------------------------------------------------------------- Resources

    /// <summary>TowerBuilt sends the tower's cost: building spends it (it used to add it).</summary>
    private void PayForTower(int cost)
    {
        ChangeMoney(-cost);
    }

    public bool CanAfford(int cost)
    {
        return TotalMoney >= cost;
    }

    /// <summary>Positive amounts grant money; negative amounts spend it.</summary>
    public void ChangeMoney(int amount)
    {
        ChangeResources(amount, 0);
    }

    /// <summary>Positive amounts grant fuel; negative amounts consume it.</summary>
    public void ChangeFuel(int amount)
    {
        ChangeResources(0, amount);
        UpdateTowerPowerStates();
    }

    public bool TryConsumeFuel(int amount)
    {
        if (amount < 0 || TotalFuel < amount) return false;
        ChangeFuel(-amount);
        return true;
    }

    /// <summary>
    /// Updates both resources together, or neither if either balance would become negative.
    /// </summary>
    public void ChangeResources(int moneyChange, int fuelChange)
    {
        long nextMoney = (long)TotalMoney + moneyChange;
        float nextFuel = TotalFuel + fuelChange;

        if (nextMoney < 0 || nextMoney > int.MaxValue || nextFuel < 0f ||
            float.IsNaN(nextFuel) || float.IsInfinity(nextFuel))
        {
            // TODO tell the player they don't have enough resources to do that
            return;
        }

        TotalMoney = (int)nextMoney;
        TotalFuel = nextFuel;
        RefreshResourceBar();
        UpdateTowerPowerStates();
    }

    public void RefreshResourceBar()
    {
        if (hp != null) hp.text = $"HP {currentHealth:0.##}";
        if (money != null) money.text = $"MONEY {TotalMoney}";
        if (fuel != null) fuel.text = $"FUEL {TotalFuel:0.##}";
    }

    // ---------------------------------------------------------------- Damage / Game Over

    /// <summary>Returns the damage actually dealt (the enemy AI learns from it).</summary>
    public float ReceiveDamage(float damage)
    {
        if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f || !IsAlive) return 0f;

        float dealt = Mathf.Min(damage, currentHealth);
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        // Keep the LevelData property in sync in case the Spawner/AI reads from it
        if (levelData != null) levelData.PlanetHealth = currentHealth;

        RefreshResourceBar();

        if (currentHealth <= 0f)
        {
            dead = true;
            if (GameManager.Instance != null) GameManager.Instance.GameOver();
        }

        return dealt;
    }

    // ---------------------------------------------------------------- Power States

    private void UpdateTowerPowerStates()
    {
        foreach (BaseTowerData tower in BaseTowerData.Active)
        {
            if (tower.fuelIntake >= 0)
            {
                if (tower.IsDisabled) tower.SetDisabled(false);
                continue;
            }

            int requiredFuel = Mathf.Abs(tower.fuelIntake);

            if (TotalFuel >= requiredFuel)
            {
                if (tower.IsDisabled) tower.SetDisabled(false);
            }
            else
            {
                if (!tower.IsDisabled) tower.SetDisabled(true);
            }
        }
    }
}
