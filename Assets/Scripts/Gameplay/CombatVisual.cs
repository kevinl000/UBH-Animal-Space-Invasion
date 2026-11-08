using UnityEngine;

// Visual-only bindings shared by gameplay prefabs. Damage always comes from combat code.
public class CombatVisual : MonoBehaviour
{
    public PooledEffect attackEffect;
    public PooledEffect revealEffect;
    public GameObject frozenVisual;
    public Vector2 emissionLocalPosition;
    public bool attackPointsUp;
    public float attackScale = 1f;
    public float revealScale = 1f;
    public float frozenScale = 1f;
    public Transform Muzzle { get; private set; }
    private Transform weapon;
    private Transform shield;
    private BaseEnemyData enemy;
    private BaseTowerData tower;
    private Animator[] animators;
    private SpriteRenderer[] renderers;
    private Color[] colors;
    private GameObject frost;
    private bool wasRevealed;

    private void Awake()
    {
        enemy = GetComponent<BaseEnemyData>();
        tower = GetComponent<BaseTowerData>();
        foreach (Transform part in GetComponentsInChildren<Transform>(true))
        {
            if (part.name == "Muzzle" || part.name == "Emission" || part.name == "CallOrigin" || part.name == "BombOutlet") Muzzle = part;
            if (part.name == "CannonPivot" || part.name == "CoilPivot") weapon = part;
            if (part.name == "Shield") shield = part;
        }
        if (Muzzle == null && emissionLocalPosition != Vector2.zero)
        {
            Muzzle = new GameObject("Emission").transform;
            Muzzle.SetParent(transform, false);
            Muzzle.localPosition = emissionLocalPosition;
        }
        animators = GetComponentsInChildren<Animator>(true);
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        colors = new Color[renderers.Length];
        for (int i = 0; i < colors.Length; i++) colors[i] = renderers[i].color;
        if (frozenVisual != null)
        {
            frost = Instantiate(frozenVisual, transform);
            frost.transform.localScale *= frozenScale;
            foreach (SpriteRenderer renderer in frost.GetComponentsInChildren<SpriteRenderer>()) renderer.sortingOrder = 105;
            frost.SetActive(false);
        }
    }

    public void ResetVisual()
    {
        wasRevealed = false;
        if (weapon != null) weapon.localRotation = Quaternion.identity;
        if (shield != null) shield.gameObject.SetActive(enemy != null && enemy.HasShield);
        if (frost != null) frost.SetActive(false);
    }

    private void LateUpdate()
    {
        if (enemy != null)
        {
            if (shield != null) shield.gameObject.SetActive(enemy.HasShield);
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = colors[i];
                if (enemy.IsShiny) color = Color.Lerp(color, new Color(1f, 0.87f, 0.35f, color.a), 0.35f);
                if (enemy.IsStealthed) color.a *= 0.22f;
                renderers[i].color = color;
            }
            if (enemy.data != null && enemy.data.isStealth && !enemy.IsStealthed && !wasRevealed)
            {
                Play(revealEffect, transform.position, transform.eulerAngles.z, transform, revealScale);
                wasRevealed = true;
            }
        }
        bool active = GameplayLoopController.CombatAllowed && (tower == null || (!tower.IsDisabled && !tower.IsFrozen));
        foreach (Animator animator in animators) animator.speed = active ? 1f : 0f;
        if (frost != null) frost.SetActive(tower != null && tower.IsAlive && tower.IsFrozen);
    }

    public void Aim(Vector2 target)
    {
        if (weapon == null || tower == null) return;
        Vector2 direction = target - (Vector2)weapon.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f - transform.eulerAngles.z;
        weapon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(Mathf.DeltaAngle(0f, angle), -tower.fov * 0.5f, tower.fov * 0.5f));
    }

    public void Attack()
    {
        Transform origin = Muzzle != null ? Muzzle : transform;
        float rotation = origin.eulerAngles.z + (tower != null && !attackPointsUp ? 90f : 0f);
        Play(attackEffect, origin.position, rotation, origin, attackScale);
    }

    public static PooledEffect Play(PooledEffect effect, Vector3 position, float rotation, Transform parent = null, float scale = 1f)
    {
        if (effect == null || PoolManager.Instance == null) return null;
        PooledEffect instance = PoolManager.Instance.Spawn(effect, parent, position, rotation);
        instance.transform.localScale = effect.transform.localScale * scale;
        if (parent != null)
        {
            Vector3 inherited = parent.lossyScale;
            Vector3 local = instance.transform.localScale;
            instance.transform.localScale = new Vector3(local.x / inherited.x, local.y / inherited.y, local.z / inherited.z);
        }
        return instance;
    }
}
