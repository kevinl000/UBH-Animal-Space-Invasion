using System;
using UnityEngine;
using UnityEngine.UI;

internal sealed class TowerPlacementPanel
{
    private const int Columns = 8;
    private const float Width = 368f;
    private const float RowHeight = 42f;

    private readonly LevelPlanetDefense defense;
    private readonly MoonOrbit moon;
    private readonly RectTransform panel;
    private readonly Font font;
    private readonly Text title;
    private readonly Text status;
    private readonly Button[] planetButtons;
    private readonly Button[] moonButtons;
    private TowerData selected;

    public TowerPlacementPanel(Transform parent, LevelPlanetDefense defense, MoonOrbit moon, Action<int, bool> place)
    {
        this.defense = defense;
        this.moon = moon;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("Quick placement canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        panel = new GameObject("Quick placement", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        panel.SetParent(root.transform, false);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0f);
        panel.anchoredPosition = new Vector2(-16f, 16f);
        panel.GetComponent<Image>().color = new Color32(22, 36, 59, 248);

        title = Label(panel, "", 12f, 8f, Width - 24f, 28f, 19, TextAnchor.MiddleLeft);
        float top = 38f;
        planetButtons = AddSlots("Planet sockets", defense.SlotCount, false, place, ref top);
        moonButtons = moon != null ? AddSlots("Moon sockets", moon.SlotCount, true, place, ref top) : Array.Empty<Button>();
        status = Label(panel, "", 12f, top, Width - 24f, 44f, 16, TextAnchor.MiddleLeft);
        panel.sizeDelta = new Vector2(Width, top + 50f);
        Hide();
    }

    public void Refresh(TowerData tower)
    {
        if (tower == null) { Hide(); return; }
        if (selected != tower)
        {
            selected = tower;
            title.text = "Quick build: " + tower.name + "  $" + tower.cost;
            ShowResult("Choose an open socket or click the planet.");
        }
        panel.gameObject.SetActive(true);
        for (int i = 0; i < planetButtons.Length; i++) planetButtons[i].interactable = defense.TowerIn(i) == null;
        for (int i = 0; i < moonButtons.Length; i++) moonButtons[i].interactable = moon.TowerIn(i) == null;
    }

    public void Hide() => panel.gameObject.SetActive(false);
    public void ShowResult(string result) => status.text = result;
    public bool Contains(Transform hit) => hit == panel || hit.IsChildOf(panel);

    private Button[] AddSlots(string label, int count, bool onMoon, Action<int, bool> place, ref float top)
    {
        Label(panel, label, 12f, top, Width - 24f, 24f, 16, TextAnchor.MiddleLeft);
        top += 24f;
        var buttons = new Button[count];
        for (int i = 0; i < count; i++)
        {
            int slot = i;
            string number = (i + 1).ToString();
            var button = new GameObject(label + " " + number, typeof(RectTransform), typeof(Image), typeof(Button));
            Position(button.GetComponent<RectTransform>(), panel, 12f + i % Columns * 43f,
                top + i / Columns * RowHeight, 38f, 36f);
            var image = button.GetComponent<Image>();
            image.color = new Color32(65, 99, 137, 255);
            buttons[i] = button.GetComponent<Button>();
            buttons[i].targetGraphic = image;
            buttons[i].onClick.AddListener(() => place(slot, onMoon));
            Label(button.transform, number, 0f, 0f, 38f, 36f, 17, TextAnchor.MiddleCenter);
        }
        top += ((count + Columns - 1) / Columns) * RowHeight;
        return buttons;
    }

    private Text Label(Transform parent, string value, float x, float y, float width, float height, int size, TextAnchor alignment)
    {
        var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        Position(label.rectTransform, parent, x, y, width, height);
        label.text = value;
        label.font = font;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = new Color32(232, 241, 255, 255);
        label.raycastTarget = false;
        return label;
    }

    private static void Position(RectTransform item, Transform parent, float x, float y, float width, float height)
    {
        item.SetParent(parent, false);
        item.anchorMin = item.anchorMax = item.pivot = new Vector2(0f, 1f);
        item.anchoredPosition = new Vector2(x, -y);
        item.sizeDelta = new Vector2(width, height);
    }
}
