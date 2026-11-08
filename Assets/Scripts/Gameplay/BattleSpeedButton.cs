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
    [Tooltip("Top-left offset in GameplayHUD's 1600x900 reference coordinates.")]
    public Vector2 position = new Vector2(242f, -36f);
    public Vector2 size = new Vector2(120f, 40f);

    // GameplayLoopController.Start registers Playing before ordinary Start methods.
    public float Speed { get; private set; } = 1f;

    private Text label;
    private readonly Color ink = new Color32(232, 241, 255, 255);
    private readonly Color idle = new Color32(38, 58, 84, 255);
    private readonly Color active = new Color32(238, 185, 77, 255);
    private Image background;
    private Button button;

    private void OnEnable() { GameManager.OnGameStateChanged += OnState; }
    private void OnDisable() { GameManager.OnGameStateChanged -= OnState; }

    public void Build(Transform parent, Font font)
    {
        if (button != null) return;
        var go = new GameObject("1x / 2x", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        background = go.GetComponent<Image>();
        background.color = idle;
        button = go.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(Toggle);

        var text = new GameObject("Label", typeof(RectTransform), typeof(Text));
        RectTransform trt = text.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        label = text.GetComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        Refresh();
    }

    public void Toggle()
    {
        if (SceneLoadingScreen.IsLoading) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing) return; // not while paused or after the result
        Speed = Speed > 1f ? 1f : fastSpeed;
        Time.timeScale = Speed;
        Refresh();
    }

    private void OnState(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing) Time.timeScale = Speed; // ResumeGame put it back to 1
        Refresh();
    }

    private void Refresh()
    {
        if (button != null) button.interactable = GameManager.Instance != null
            && GameManager.Instance.CurrentState == GameManager.GameState.Playing;
        if (label != null) { label.text = Speed > 1f ? "2x  >>" : "1x  >"; label.color = Speed > 1f ? idle : ink; }
        if (background != null) background.color = Speed > 1f ? active : idle;
    }

    private void OnDestroy()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.CurrentState == GameManager.GameState.Playing) Time.timeScale = 1f; // don't carry 2x into the next scene
    }
}
