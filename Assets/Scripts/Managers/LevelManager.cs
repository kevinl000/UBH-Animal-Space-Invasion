using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class LevelManager : MonoBehaviour, IDamagable
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
        
    }

    private void OnDisable()
    {
        
    }


    private void FixedUpdate()
    {
        if (currentHealth <= 0f) return;

        float direction = clockwise ? -1f : 1f;
        planetBody.MoveRotation(planetBody.rotation + direction * rotationSpeed * Time.fixedDeltaTime);
    }

    // Attach to the planet's Rigidbody2D object, with a non-trigger Collider2D.
    // This callback runs when contact begins, not every frame of continuing contact.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActiveAndEnabled || currentHealth <= 0f) return;

        // Also support a child collider attached to a tagged enemy Rigidbody2D.
        bool isEnemy = collision.collider.CompareTag("Enemy") ||
            (collision.rigidbody != null && collision.rigidbody.CompareTag("Enemy"));
        if (!isEnemy) return;

        var enemy = collision.collider.GetComponentInParent<IDamagable>();
        if (enemy == null) return;

        if (currentHealth <= 0f && GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    /// <summary>Positive amounts grant money; negative amounts spend it. False means insufficient funds.</summary>
    public bool ChangeMoney(int amount)
    {
        return ChangeResources(amount, 0f);
    }

    /// <summary>Positive amounts grant fuel; negative amounts consume it. False means insufficient fuel.</summary>
    public bool ChangeFuel(float amount)
    {
        return ChangeResources(0, amount);
    }

    /// <summary>
    /// Updates both resources together, or neither if either balance would become negative.
    /// Enemies and towers call this on their own schedule (including once per second).
    /// </summary>
    public bool ChangeResources(int moneyChange, float fuelChange)
    {
        long nextMoney = (long)TotalMoney + moneyChange;
        float nextFuel = TotalFuel + fuelChange;
        if (nextMoney < 0 || nextMoney > int.MaxValue || nextFuel < 0f ||
            float.IsNaN(nextFuel) || float.IsInfinity(nextFuel))
            return false;

        TotalMoney = (int)nextMoney;
        TotalFuel = nextFuel;
        RefreshResourceBar();
        return true;
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

// The enemy implementation can supply damage without managers referencing its assembly.
public interface IDamagable
{
    bool IsAlive { get; }
    float ReceiveDamage(float damage);
}
