using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Native overlay UI; the battle remains world-space so camera moves cannot move the HUD.
public class GameplayHUD : MonoBehaviour
{
    public GameplayLoopController loop;
    public Font font;
    public Sprite[] towerPortraits;
    public AudioClip confirmSound;
    private Text resources, phase, boss, details;
    private Button start;
    private Button[] cards;
    private GameObject modal;
    private string currentModal = "";
    private Canvas canvas;
    private RectTransform sidebar;
    private int screenWidth, screenHeight;
    private float nextRefresh;
    private readonly Color ink = new Color32(232, 241, 255, 255);
    private readonly Color panel = new Color32(22, 36, 59, 255);
    private readonly Color gold = new Color32(238, 185, 77, 255);

    private void Start()
    {
        if (EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        var root = new GameObject("Union Prime HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;
        sidebar = Box("Tower panel", root.transform, new Vector2(0f, 0.5f), new Vector2(180f, 0f), new Vector2(360f, 900f), panel);
        sidebar.anchorMin = new Vector2(0, 0);
        sidebar.anchorMax = new Vector2(0, 1);
        sidebar.sizeDelta = new Vector2(360f, 0);
        Label("UNION PRIME", sidebar, new Vector2(0, -36), new Vector2(324, 42), 30, gold);
        Label("CAMPUS DEFENSE / TUTORIAL", sidebar, new Vector2(0, -73), new Vector2(324, 32), 17, ink);
        resources = Label("", sidebar, new Vector2(0, -132), new Vector2(324, 72), 24, ink);
        phase = Label("", sidebar, new Vector2(0, -215), new Vector2(324, 72), 23, gold);
        cards = new Button[loop.config.towers.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            TowerData data = loop.config.towers[i];
            cards[i] = Button(data.name + "  $" + data.cost, sidebar, new Vector2(0, -290 - i * 66), new Vector2(324, 58), () =>
            {
                loop.placement.SelectTower(data);
                PlayClick();
            });
            if (towerPortraits != null && i < towerPortraits.Length && towerPortraits[i] != null)
            {
                var portrait = Box("Portrait", cards[i].transform, new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(50, 50), Color.white);
                Image image = portrait.GetComponent<Image>();
                image.sprite = towerPortraits[index];
                image.preserveAspect = true;
                image.raycastTarget = false;
                var label = cards[i].GetComponentInChildren<Text>();
                label.rectTransform.offsetMin = new Vector2(55, 0);
            }
        }
        details = Label("", sidebar, new Vector2(0, -583), new Vector2(324, 115), 19, ink);
        loop.placement.message = Label("Click a socket to build.\nRight-click a tower to sell.", sidebar, new Vector2(0, -710), new Vector2(324, 100), 19, ink);
        start = Button("START WAVE 1", sidebar, new Vector2(0, -810), new Vector2(324, 56), () => { PlayClick(); loop.StartGame(); });
        start.GetComponent<Image>().color = gold;
        start.GetComponentInChildren<Text>().color = panel;
        boss = Label("", root.transform, new Vector2(180, -38), new Vector2(650, 40), 25, gold);
        Button("PAUSE", root.transform, new Vector2(0, -90), new Vector2(160, 40), () => GameManager.Instance.PauseGame()).GetComponent<RectTransform>().anchoredPosition = new Vector2(180, -90);
        FitCamera();
        EventSystem.current.SetSelectedGameObject(start.gameObject);
    }

    private void Update()
    {
        if (canvas == null) return;
        if (Screen.width != screenWidth || Screen.height != screenHeight)
            if (loop.Phase != GameplayLoopController.GameplayPhase.BossEntrance) FitCamera();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !SceneLoadingScreen.IsLoading)
        {
            if (loop.Paused) GameManager.Instance.ResumeGame();
            else GameManager.Instance.PauseGame();
        }
        string desired = loop.Phase == GameplayLoopController.GameplayPhase.Victory ? "VICTORY"
            : loop.Phase == GameplayLoopController.GameplayPhase.Defeat ? "DEFEAT" : loop.Paused ? "PAUSED" : "";
        if (desired != currentModal) ShowModal(desired);
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.1f;
        resources.text = "HP " + loop.levelManager.PlanetHP.ToString("0") + " / " + loop.config.level.planetMaxHealth.ToString("0")
            + "\n$ " + loop.levelManager.TotalMoney + "     FUEL " + loop.levelManager.TotalFuel.ToString("0");
        phase.text = PhaseText();
        BaseEnemyData final = loop.FinalBoss;
        boss.text = final != null && final.IsAlive ? final.data.name.ToUpperInvariant() + "   " + final.Health.ToString("0") + " / " + final.MaxHealth.ToString("0") : "";
        bool preparing = loop.Phase == GameplayLoopController.GameplayPhase.Setup;
        if (!preparing && start.gameObject.activeSelf && EventSystem.current.currentSelectedGameObject == start.gameObject)
            EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        start.gameObject.SetActive(preparing);
        start.interactable = loop.CanBuild;
        TowerData selected = loop.placement.Selected;
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].interactable = loop.CanBuild;
            cards[i].GetComponent<Image>().color = loop.config.towers[i] == selected ? new Color32(65, 99, 137, 255) : new Color32(38, 58, 84, 255);
        }
        if (selected != null)
            details.text = selected.name.ToUpperInvariant() + "  /  " + selected.attackType + "\nHP " + selected.maxHealth + "   DAMAGE " + selected.damage
                + (selected.attackType == TowerAttackType.Dot ? "/s × 3s" : "") + "\nFUEL " + selected.fuelIntake + "/s   ARC " + selected.fov + "°";
    }

    private string PhaseText()
    {
        switch (loop.Phase)
        {
            case GameplayLoopController.GameplayPhase.Setup: return "PREPARE YOUR DEFENSE\nBuild, then press Start";
            case GameplayLoopController.GameplayPhase.Wave: return "WAVE " + loop.WaveNumber + " / " + loop.config.waves.Length + "\nSpawning: " + Mathf.CeilToInt(loop.Remaining) + "s";
            case GameplayLoopController.GameplayPhase.Clear: return "MINIBOSS / CLEAR THE SKY\n" + BaseEnemyData.Active.Count + " enemies remaining";
            case GameplayLoopController.GameplayPhase.Break: return "BUILD & SELL / FULL REFUND\nNext wave: " + Mathf.CeilToInt(loop.Remaining) + "s";
            case GameplayLoopController.GameplayPhase.BossEntrance: return "FINAL BOSS INCOMING";
            case GameplayLoopController.GameplayPhase.BossFight: return "FINAL BOSS\nDefend Union Prime!";
            default: return "UNION PRIME";
        }
    }

    private void FitCamera()
    {
        Canvas.ForceUpdateCanvases();
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        float left = Mathf.Clamp(360f * canvas.scaleFactor / Mathf.Max(1, screenWidth), 0f, 0.65f);
        loop.battleCamera.rect = new Rect(left, 0, 1f - left, 1f);
        float aspect = Mathf.Max(0.1f, screenWidth * (1f - left) / Mathf.Max(1, screenHeight));
        loop.battleCamera.orthographicSize = Mathf.Max(8.7f, 8.7f / aspect);
        loop.battleCamera.transform.position = new Vector3(0, 0, -10);
    }

    private void ShowModal(string title)
    {
        currentModal = title;
        if (modal != null) { modal.SetActive(false); Destroy(modal); }
        if (title == "") { EventSystem.current.SetSelectedGameObject(start.gameObject.activeInHierarchy ? start.gameObject : cards[0].gameObject); return; }
        RectTransform shade = Box(title, canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.78f));
        shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
        modal = shade.gameObject;
        RectTransform window = Box("Result", shade, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 410), panel);
        Label(title == "VICTORY" ? "UNION PRIME DEFENDED!" : title, window, new Vector2(0, -65), new Vector2(500, 60), 32, gold);
        bool won = title == "VICTORY";
        Button primary = Button(won ? "NEXT LEVEL  →  CAPEN MAJOR" : title == "PAUSED" ? "RESUME" : "RETRY", window,
            new Vector2(0, -170), new Vector2(450, 64), () =>
            {
                PlayClick();
                if (won) GameManager.Instance.StartLevel(loop.config.nextScene);
                else if (title == "PAUSED") GameManager.Instance.ResumeGame();
                else GameManager.Instance.RestartCurrentLevel();
            });
        primary.GetComponent<Image>().color = gold;
        primary.GetComponentInChildren<Text>().color = panel;
        if (title == "PAUSED") Button("RETRY", window, new Vector2(0, -250), new Vector2(450, 48), () => GameManager.Instance.RestartCurrentLevel());
        Button("TITLE", window, new Vector2(0, -330), new Vector2(450, 48), () => GameManager.Instance.GoToMainMenu());
        EventSystem.current.SetSelectedGameObject(primary.gameObject);
    }

    private void PlayClick() { if (AudioManager.Instance != null && confirmSound != null) AudioManager.Instance.PlaySFX(confirmSound, 0.65f); }

    private RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = position; rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    private Text Label(string text, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.anchoredPosition = position; rt.sizeDelta = size;
        var label = go.GetComponent<Text>(); label.font = font; label.fontSize = fontSize; label.color = color;
        label.alignment = TextAnchor.MiddleCenter; label.text = text; label.raycastTarget = false;
        return label;
    }

    private Button Button(string text, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform rt = Box(text, parent, new Vector2(0.5f, 1f), position, size, new Color32(38, 58, 84, 255));
        var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = rt.GetComponent<Image>(); button.onClick.AddListener(action);
        Text label = Label(text, rt, Vector2.zero, size, 22, ink);
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.sizeDelta = Vector2.zero; label.rectTransform.anchoredPosition = Vector2.zero;
        return button;
    }
}
