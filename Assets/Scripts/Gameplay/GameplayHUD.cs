using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Native overlay UI; the battle remains world-space so camera moves cannot move the HUD.
public class GameplayHUD : MonoBehaviour
{
    public GameplayLoopController loop;
    [Min(0.1f)] public float worldMagnification = 1f;
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
    private string planetTitle;
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
        planetTitle = loop.config.displayName.ToUpperInvariant();
        var root = new GameObject(loop.config.displayName + " HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;
        BuildStars();
        sidebar = Box("Tower panel", root.transform, new Vector2(1f, 0.5f), new Vector2(-180f, 0f), new Vector2(360f, 900f), panel);
        sidebar.anchorMin = new Vector2(1, 0);
        sidebar.anchorMax = new Vector2(1, 1);
        sidebar.offsetMin = new Vector2(-360f, TowerPlacementPanel.Height);
        sidebar.offsetMax = Vector2.zero;
        Label(planetTitle, sidebar, new Vector2(0, -28), new Vector2(324, 38), 30, gold);
        Label(loop.config.stageLabel, sidebar, new Vector2(0, -62), new Vector2(324, 28), 17, ink);
        resources = Label("", sidebar, new Vector2(0, -114), new Vector2(324, 66), 24, ink);
        phase = Label("", sidebar, new Vector2(0, -186), new Vector2(324, 66), 23, gold);
        cards = new Button[loop.config.towers.Length];
        bool compactCards = cards.Length > 4;
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            TowerData data = loop.config.towers[i];
            Vector2 position = compactCards ? new Vector2(i % 2 == 0 ? -83 : 83, -252 - i / 2 * 62) : new Vector2(0, -252 - i * 62);
            Vector2 size = new Vector2(compactCards ? 158 : 324, 54);
            cards[i] = Button(data.name + (compactCards ? "\n$" : "  $") + data.cost, sidebar, position, size, () =>
            {
                loop.placement.SelectTower(data);
                PlayClick();
            });
            if (compactCards) cards[i].GetComponentInChildren<Text>().fontSize = 18;
            if (towerPortraits != null && i < towerPortraits.Length && towerPortraits[i] != null)
            {
                float portraitSize = compactCards ? 38 : 50;
                var portrait = Box("Portrait", cards[i].transform, new Vector2(0f, 0.5f), new Vector2(compactCards ? 23 : 30, 0), new Vector2(portraitSize, portraitSize), Color.white);
                Image image = portrait.GetComponent<Image>();
                image.sprite = towerPortraits[index];
                image.preserveAspect = true;
                image.raycastTarget = false;
                var label = cards[i].GetComponentInChildren<Text>();
                label.rectTransform.offsetMin = new Vector2(compactCards ? 45 : 55, 0);
            }
        }
        details = Label("", sidebar, new Vector2(0, -536), new Vector2(324, 100), 19, ink);
        loop.placement.message = Label("Click a socket to build.\nRight-click a tower to sell.", sidebar, new Vector2(0, -640), new Vector2(324, 84), 19, ink);
        start = Button("START WAVE 1", sidebar, new Vector2(0, -738), new Vector2(324, 56), () => { PlayClick(); loop.StartGame(); });
        start.GetComponent<Image>().color = gold;
        start.GetComponentInChildren<Text>().color = panel;
        boss = Label("", root.transform, new Vector2(-180, -86), new Vector2(650, 40), 25, gold);
        RectTransform pause = Button("PAUSE", root.transform, new Vector2(96, -36), new Vector2(160, 40),
            () => GameManager.Instance.PauseGame()).GetComponent<RectTransform>();
        pause.anchorMin = pause.anchorMax = new Vector2(0f, 1f);
        BattleSpeedButton speed = loop.GetComponent<BattleSpeedButton>();
        if (speed != null) speed.Build(root.transform, font);
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
        {
            string stats = "HP " + selected.maxHealth;
            if (selected.kind == EnemyAI.TowerKind.Radar) stats += "\nREVEALS STEALTH ENEMIES";
            else if (selected.fuelIntake > 0) stats += "\nGENERATES +" + selected.fuelIntake + " FUEL/s";
            else if (selected.prefab is ResourceTower resource)
                stats += "\nGENERATES $" + (resource.generationAmount / (selected.fireRate > 0 ? selected.fireRate : 1f)).ToString("0.##") + "/s";
            else stats += "   DAMAGE " + selected.damage + (selected.attackType == TowerAttackType.Dot ? "/s × 3s" : "");
            details.text = selected.name.ToUpperInvariant() + "  /  " + (selected.attackType == TowerAttackType.None ? "SUPPORT" : selected.attackType.ToString()) + "\n" + stats + "\nFUEL "
                + (selected.fuelIntake == 0 ? "FREE" : selected.fuelIntake + (selected.fuelIntake > 0 ? "/s"
                    : selected.kind == EnemyAI.TowerKind.GammaRadar ? "/pulse" : "/shot")) + "   ARC " + selected.fov + "°";
        }
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
            case GameplayLoopController.GameplayPhase.BossFight: return "FINAL BOSS\nDefend " + loop.config.displayName + "!";
            default: return planetTitle;
        }
    }

    private void FitCamera()
    {
        Canvas.ForceUpdateCanvases();
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        float right = Mathf.Clamp(360f * canvas.scaleFactor / Mathf.Max(1, screenWidth), 0f, 0.65f);
        float bottom = Mathf.Clamp(TowerPlacementPanel.Height * canvas.scaleFactor / Mathf.Max(1, screenHeight), 0f, 0.3f);
        loop.battleCamera.rect = new Rect(0, bottom, 1f - right, 1f - bottom);
        float aspect = Mathf.Max(0.1f, screenWidth * (1f - right) / Mathf.Max(1f, screenHeight * (1f - bottom)));
        float fittedSize = Mathf.Max(8.7f, 8.7f / aspect) / Mathf.Max(0.1f, worldMagnification);
        PlanetPageZoom zoom = loop.GetComponent<PlanetPageZoom>();
        if (zoom != null) zoom.SetFittedCameraSize(fittedSize);
        else loop.battleCamera.orthographicSize = fittedSize;
        loop.battleCamera.transform.position = new Vector3(0, 0, -10);
    }

    private void BuildStars()
    {
        // Reuse the old PlanetHUD's fixed pixel stars, behind the world sprites.
        var sky = new GameObject("Stars", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        sky.transform.SetParent(transform, false);
        Canvas backdrop = sky.GetComponent<Canvas>();
        backdrop.renderMode = RenderMode.ScreenSpaceCamera;
        backdrop.worldCamera = loop.battleCamera;
        backdrop.planeDistance = 50f;
        backdrop.sortingOrder = -100;
        CanvasScaler scaler = sky.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;
        for (int i = 0; i < 32; i++)
        {
            float size = i % 4 == 0 ? 4f : 2f;
            RectTransform star = Box("Star" + i, sky.transform,
                new Vector2((i * 211 % 1600 + 36) / 1672f, (i * 137 % 880 + 30) / 941f),
                Vector2.zero, new Vector2(size, size), new Color32(61, 82, 112, 255));
            star.GetComponent<Image>().raycastTarget = false;
        }
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
        Label(title == "VICTORY" ? planetTitle + " DEFENDED!" : title, window, new Vector2(0, -65), new Vector2(500, 60), 32, gold);
        bool won = title == "VICTORY";
        bool lastLevel = string.IsNullOrEmpty(loop.config.nextScene);
        Button primary = Button(won ? lastLevel ? "LEVEL SELECT" : "NEXT LEVEL  →  " + loop.config.nextPlanetName.ToUpperInvariant() : title == "PAUSED" ? "RESUME" : "RETRY", window,
            new Vector2(0, -170), new Vector2(450, 64), () =>
            {
                PlayClick();
                if (won)
                {
                    if (lastLevel) GameManager.Instance.GoToLevelSelect();
                    else GameManager.Instance.StartLevel(loop.config.nextScene);
                }
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
