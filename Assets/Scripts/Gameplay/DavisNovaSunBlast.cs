using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One shared combat clock: newly built and unpowered towers face the same blast.
public class DavisNovaSunBlast : MonoBehaviour
{
    public GameplayLoopController loop;
    public SpriteRenderer sun;
    public Sprite[] explosionFrames;
    [Min(1f)] public float explosionRadius = 6f;
    [Min(1f)] public float intervalSeconds = 40f;
    [Min(0f)] public float warningSeconds = 5f;
    public float Remaining { get; private set; }
    public int BlastCount { get; private set; }

    private readonly List<BaseTowerData> targets = new List<BaseTowerData>(12);
    private Text countdown;
    private SpriteRenderer explosion;
    private Vector3 sunScale;
    private Color sunColor;
    private float explosionElapsed = -1f;
    private float nextLabelRefresh;
    private const float FrameSeconds = 0.1f;
    private const float IgnitionSeconds = 0.3f;
    // The peak frame contains an opaque disk of radius 110px at 100 PPU.
    private const float PeakCoreRadius = 1.1f;

    private void Awake()
    {
        Remaining = Mathf.Max(1f, intervalSeconds);
        if (sun == null) return;
        sunScale = sun.transform.localScale;
        sunColor = sun.color;
        explosion = new GameObject("Supernova", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        explosion.transform.SetParent(transform, false);
        explosion.transform.position = sun.transform.position;
        explosion.transform.localScale = Vector3.one * (explosionRadius / PeakCoreRadius);
        explosion.sortingLayerID = sun.sortingLayerID;
        explosion.sortingOrder = 110;
        explosion.enabled = false;
    }

    private void Start()
    {
        var root = new GameObject("Sun blast countdown", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // Shared pause/results UI covers this label.
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;
        countdown = new GameObject("Countdown", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        countdown.transform.SetParent(root.transform, false);
        RectTransform rect = countdown.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(-180f, -42f);
        rect.sizeDelta = new Vector2(480f, 64f);
        countdown.font = GetComponent<GameplayHUD>().font;
        countdown.fontSize = 21;
        countdown.alignment = TextAnchor.MiddleCenter;
        countdown.raycastTarget = false;
        RefreshLabel();
    }

    private void Update()
    {
        if (loop == null || !loop.Ready) return;
        if (loop.Simulating && loop.levelManager.IsAlive)
        {
            float dt = Time.deltaTime;
            Remaining -= dt;
            if (explosionElapsed >= 0f) explosionElapsed += dt;
            if (Remaining <= 0f)
            {
                Remaining += Mathf.Max(1f, intervalSeconds);
                Blast();
            }
            else if (Remaining <= IgnitionSeconds && explosionElapsed < 0f)
                explosionElapsed = IgnitionSeconds - Remaining;
            UpdateSun();
        }
        if (explosion != null && (loop.Phase == GameplayLoopController.GameplayPhase.Victory
            || loop.Phase == GameplayLoopController.GameplayPhase.Defeat)) explosion.enabled = false;
        if (Time.unscaledTime < nextLabelRefresh) return;
        nextLabelRefresh = Time.unscaledTime + 0.1f;
        RefreshLabel();
    }

    private void Blast()
    {
        // Damage uses the normal death/pool path, so sockets free without refunds.
        targets.Clear();
        for (int slot = 0; slot < loop.defense.SlotCount; slot++)
        {
            BaseTowerData tower = loop.defense.TowerIn(slot);
            if (tower != null) targets.Add(tower);
        }
        // Snapshot before damage mutates the active list. All hits run in this frame,
        // regardless of age, power, freezing or the visual shockwave's distance.
        foreach (BaseTowerData tower in targets)
            if (tower != null && tower.IsAlive)
                tower.ReceiveDamage(tower.Health);
        targets.Clear();
        BlastCount++;
        explosionElapsed = IgnitionSeconds;
    }

    private void UpdateSun()
    {
        if (sun == null) return;
        float warning = Remaining <= warningSeconds && warningSeconds > 0f
            ? (Mathf.Sin(Remaining * 10f) + 1f) * 0.5f : 0f;
        sun.color = Color.Lerp(sunColor, new Color(1f, 0.45f, 0.2f), warning * 0.65f);
        sun.transform.localScale = sunScale * (1f + warning * 0.04f);
        if (explosion == null) return;
        int frame = Mathf.FloorToInt(explosionElapsed / FrameSeconds);
        explosion.enabled = explosionFrames != null && frame >= 0 && frame < explosionFrames.Length;
        if (explosion.enabled) explosion.sprite = explosionFrames[frame];
        else explosionElapsed = -1f;
    }

    private void RefreshLabel()
    {
        if (countdown == null || loop == null) return;
        bool visible = !loop.Paused && !SceneLoadingScreen.IsLoading
            && loop.Phase != GameplayLoopController.GameplayPhase.Victory
            && loop.Phase != GameplayLoopController.GameplayPhase.Defeat;
        countdown.gameObject.SetActive(visible);
        bool warning = Remaining <= warningSeconds;
        countdown.color = warning ? new Color32(255, 113, 77, 255) : new Color32(238, 185, 77, 255);
        countdown.text = "SUN BLAST IN " + Mathf.CeilToInt(Remaining) + "s"
            + (loop.Simulating ? "" : " / CLOCK HELD") + "\nALL TOWERS WILL BE DESTROYED";
    }

    private void OnDisable()
    {
        if (sun != null) { sun.color = sunColor; sun.transform.localScale = sunScale; }
        if (explosion != null) explosion.enabled = false;
        if (countdown != null) countdown.gameObject.SetActive(false);
    }
}
