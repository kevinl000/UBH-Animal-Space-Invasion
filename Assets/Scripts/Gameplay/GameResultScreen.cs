using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Win / lose screen for a level. Shows when GameManager switches to LevelComplete or GameOver
/// (or, if the level was opened directly without a GameManager, when the planet's HP hits 0 / the level is cleared).
/// Builds its own overlay canvas at runtime using the HUD's font, so nothing in the scene changes. Added by GameplayBootstrap.
/// </summary>
public class GameResultScreen : MonoBehaviour
{
    public Color panelColor = new Color(0.02f, 0.04f, 0.1f, 0.85f);
    public Color winColor = new Color(0.58f, 0.86f, 0.52f);
    public Color loseColor = new Color(1f, 0.45f, 0.4f);

    public bool Shown { get; private set; }
    public bool Won { get; private set; }

    private LevelManager level;
    private EnemySpawner spawner;
    private Font font;
    private int kills;
    private bool timeUp;
    private GameObject root;

    private void Awake()
    {
        level = FindAnyObjectByType<LevelManager>();
        spawner = FindAnyObjectByType<EnemySpawner>();
        if (level != null && level.hp != null) font = level.hp.font; // match the HUD's pixel font
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += OnState;
        EnemyEvents.Killed += OnKilled;
        EnemyEvents.LevelTimeUp += OnTimeUp;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnState;
        EnemyEvents.Killed -= OnKilled;
        EnemyEvents.LevelTimeUp -= OnTimeUp;
    }

    private void OnKilled(int money, int fuel) { kills++; }
    private void OnTimeUp() { timeUp = true; }

    private void OnState(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver) Show(false);
        else if (state == GameManager.GameState.LevelComplete) Show(true);
    }

    // fallback when there's no GameManager (level opened directly in the editor)
    private void Update()
    {
        if (Shown || GameManager.Instance != null) return;
        if (level != null && !level.IsAlive) { Time.timeScale = 0f; Show(false); }
        else if (timeUp && BaseEnemyData.Active.Count == 0) { Time.timeScale = 0f; Show(true); }
    }

    public void Show(bool won)
    {
        if (Shown) return;
        Shown = true;
        Won = won;
        Build(won);
    }

    private void Build(bool won)
    {
        root = new GameObject("Result Screen", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // above the HUD
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672, 941); // same as the HUD
        root.AddComponent<GraphicRaycaster>();

        Image dim = Box(root.transform, "Dim", Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
        Stretch(dim.rectTransform);
        Image panel = Box(root.transform, "Panel", Vector2.zero, new Vector2(620, 380), panelColor);

        string time = spawner != null ? Clock(spawner.Elapsed) : "-";
        Label(panel.transform, won ? "PLANET DEFENDED" : "PLANET LOST", new Vector2(0, 120), 46, won ? winColor : loseColor);
        Label(panel.transform, won ? "Every wave is cleared." : "The animals took the planet.", new Vector2(0, 62), 22, Color.white);
        string stats = "ENEMIES DEFEATED  " + kills + "\nTIME  " + time;
        if (level != null) stats += "\nPLANET HP  " + Mathf.CeilToInt(level.PlanetHP) + "    MONEY  " + level.TotalMoney;
        Label(panel.transform, stats, new Vector2(0, -12), 22, new Color(0.75f, 0.85f, 1f));

        Button retry = MakeButton(panel.transform, "RETRY", new Vector2(-140, -130));
        retry.onClick.AddListener(Retry);
        Button select = MakeButton(panel.transform, "LEVEL SELECT", new Vector2(140, -130));
        select.onClick.AddListener(LevelSelect);
    }

    public void Retry()
    {
        if (SceneLoadingScreen.IsLoading) return;
        GameManager gm = GameManager.Instance;
        if (gm != null && !string.IsNullOrEmpty(gm.LastPlayedLevel)) { gm.RestartCurrentLevel(); return; }
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // opened directly in the editor
    }

    public void LevelSelect()
    {
        if (SceneLoadingScreen.IsLoading) return;
        GameManager gm = GameManager.Instance;
        if (gm != null && SceneController.Instance != null) { gm.GoToLevelSelect(); return; }
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelect");
    }

    // ---------------------------------------------------------------- tiny UI helpers

    private static string Clock(float seconds)
    {
        return Mathf.FloorToInt(seconds / 60f) + ":" + Mathf.FloorToInt(seconds % 60f).ToString("00");
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Image Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    private Text Label(Transform parent, string text, Vector2 pos, int size, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        RectTransform rt = t.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(580, 60);
        return t;
    }

    private Button MakeButton(Transform parent, string text, Vector2 pos)
    {
        Image img = Box(parent, text, pos, new Vector2(230, 56), new Color(0.12f, 0.2f, 0.38f, 1f));
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        Label(img.transform, text, Vector2.zero, 22, Color.white).rectTransform.sizeDelta = new Vector2(230, 56);
        return b;
    }
}
