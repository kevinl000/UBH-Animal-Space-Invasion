using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class LevelManager : MonoBehaviour, IDamageable
{
    public Text hp;
    public Text money;
    public Text fuel;
    public Text time;
    public Text wave;


    [Header("Starting resources")]
    [SerializeField, Min(0f)] private float startingHP = 100f;
    [SerializeField, Min(0)] private int startingMoney;
    [SerializeField, Min(0f)] private float startingFuel;

    [Header("Planet rotation")]
    [Tooltip("Degrees per second. Freeze Rigidbody2D position, but leave rotation unfrozen.")]
    [SerializeField, Min(0f)] private float rotationSpeed = 10f;
    [SerializeField] private bool clockwise = true;

    public float PlanetHP { get { return currentHealth; } }
    public int TotalMoney { get; private set; }
    public float TotalFuel { get; private set; }

    private float currentHealth;
    private bool dead;


    public bool IsAlive { get { return !dead && currentHealth > 0f; } }


    private Rigidbody2D planetBody;

    private void Awake()
    {
        planetBody = GetComponent<Rigidbody2D>();
        currentHealth = Mathf.Max(0f, startingHP);
        TotalMoney = Mathf.Max(0, startingMoney);
        TotalFuel = Mathf.Max(0f, startingFuel);
        RefreshResourceBar();
    }

    private void OnEnable()
    {
        EnemyEvents.Killed += ChangeResources;
        TowerEvents.TowerBuilt += ChangeMoney;
        TowerEvents.TowerSold += ChangeMoney;
    }

    private void OnDisable()
    {
        EnemyEvents.Killed -= ChangeResources;
        TowerEvents.TowerBuilt -= ChangeMoney;
        TowerEvents.TowerSold -= ChangeMoney;
    }

    private void FixedUpdate()
    {
        if (currentHealth <= 0f) return;

        float direction = clockwise ? -1f : 1f;
        planetBody.MoveRotation(planetBody.rotation + direction * rotationSpeed * Time.fixedDeltaTime);
    }


    /// <summary>Positive amounts grant money; negative amounts spend it. False means insufficient funds.</summary>
    public void ChangeMoney(int amount)
    {
        ChangeResources(amount, 0);
    }

    /// <summary>Positive amounts grant fuel; negative amounts consume it. False means insufficient fuel.</summary>
    public void ChangeFuel(int amount)
    {
        ChangeResources(0, amount);
    }

    /// <summary>
    /// Updates both resources together, or neither if either balance would become negative.
    /// Enemies and towers call this on their own schedule (including once per second).
    /// </summary>
    public void ChangeResources(int moneyChange, int fuelChange)
    {
        long nextMoney = (long)TotalMoney + moneyChange;
        float nextFuel = TotalFuel + fuelChange;
        if (nextMoney < 0 || nextMoney > int.MaxValue || nextFuel < 0f ||
            float.IsNaN(nextFuel) || float.IsInfinity(nextFuel))
        { return; }//TODO tell the player they don't have enough resources to do that
        TotalMoney = (int)nextMoney;
        TotalFuel = nextFuel;
        RefreshResourceBar();
    }

    public void RefreshResourceBar()
    {
        if (hp != null) hp.text = $"HP {currentHealth:0.##}";
        if (money != null) money.text = $"MONEY {TotalMoney}";
        if (fuel != null) fuel.text = $"FUEL {TotalFuel:0.##}";
    }

    public float ReceiveDamage(float damage)
    {
        if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return damage;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        RefreshResourceBar();
        if (currentHealth <= 0f && GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        return currentHealth;
    }
}
