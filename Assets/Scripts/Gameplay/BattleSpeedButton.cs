using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A "1x / 2x" game speed button next to the GameplayHUD's PAUSE button (Union Prime).
/// Speed only applies while the game is Playing; pausing and resuming keep the chosen speed
/// (GameManager.ResumeGame resets timeScale to 1, so it's re-applied). Added at runtime by GameplayBootstrap.
/// </summary>
public class BattleSpeedButton : MonoBehaviour
{
    public float fastSpeed = 2f;
    [Tooltip("Same layout as GameplayHUD: anchored at the top centre, 1600x900 reference.")]
    public Vector2 position = new Vector2(-80f, -90f);
    public Vector2 size = new Vector2(120f, 40f);

    public float Speed { get; private set; }

    private Text label;
    private readonly Color ink = new Color32(232, 241, 255, 255);
    private readonly Color idle = new Color32(38, 58, 84, 255);
    private readonly Color active = new Color32(238, 185, 77, 255);
    private Image background;

    private void OnEnable() { GameManager.OnGameStateChanged += OnState; }
    private void OnDisable() { GameManager.OnGameStateChanged -= OnState; }

    private void Start()
    {
        Speed = 1f;
        GameplayHUD hud = FindAnyObjectByType<GameplayHUD>();
        Font font = hud != null && hud.font != null ? hud.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("Speed Button Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // just under the GameplayHUD (100), so its pause / result screens cover it
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;

        var go = new GameObject("1x / 2x", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(root.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        background = go.GetComponent<Image>();
        background.color = idle;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(Toggle);

        var text = new GameObject("Label", typeof(RectTransform), typeof(Text));
        RectTransform trt = text.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        label = text.GetComponent<Text>();
        label.font = font;
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        Refresh();
    }

    public void Toggle()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing) return; // not while paused or after the result
        Speed = Speed > 1f ? 1f : fastSpeed;
        Time.timeScale = Speed;
        Refresh();
    }

    private void OnState(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing) Time.timeScale = Speed; // ResumeGame put it back to 1
    }

    private void Refresh()
    {
        if (label != null) { label.text = Speed > 1f ? "2x  >>" : "1x  >"; label.color = Speed > 1f ? idle : ink; }
        if (background != null) background.color = Speed > 1f ? active : idle;
    }

    private void OnDestroy()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.CurrentState == GameManager.GameState.Playing) Time.timeScale = 1f; // don't carry 2x into the next scene
    }
}
