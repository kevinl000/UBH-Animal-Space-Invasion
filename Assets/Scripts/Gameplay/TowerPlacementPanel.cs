using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class TowerPlacementPanel
{
    public const float Height = 96f;

    private readonly LevelPlanetDefense defense;
    private readonly MoonOrbit moon;
    private readonly RectTransform panel;
    private readonly Font font;
    private readonly Text title;
    private readonly Text status;
    private readonly Button[] planetButtons;
    private readonly Button[] moonButtons;
    private readonly TowerPlacementPreview preview;
    private TowerData selected;
    private int hoveredSlot = -1;
    private bool hoveredMoon;

    public TowerPlacementPanel(Transform parent, LevelPlanetDefense defense, MoonOrbit moon, Font font, Action<int, bool> place)
    {
        this.defense = defense;
        this.moon = moon;
        this.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        preview = new TowerPlacementPreview(defense.transform);

        var root = new GameObject("Quick placement canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;

        panel = Box("Quick build", root.transform, new Color32(12, 21, 37, 255));
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = new Vector2(1f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.sizeDelta = new Vector2(0f, Height);
        panel.anchoredPosition = Vector2.zero;
        var trim = Box("Pixel trim", panel, new Color32(238, 185, 77, 255));
        Stretch(trim, 8f, 2f, 8f, Height - 5f);
        title = Label(panel, "", 18, TextAnchor.MiddleLeft);
        Stretch(title.rectTransform, 16f, 6f, 16f, Height - 28f);

        var row = new GameObject("Socket row", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        row.SetParent(panel, false);
        Stretch(row, 12f, 32f, 12f, 22f);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;
        planetButtons = AddSlots(row, defense.SlotCount, false, place);
        moonButtons = moon != null ? AddSlots(row, moon.SlotCount, true, place) : Array.Empty<Button>();
        status = Label(panel, "", 14, TextAnchor.MiddleLeft);
        Stretch(status.rectTransform, 16f, Height - 21f, 16f, 1f);
        Hide();
    }

    public void Refresh(TowerData tower, int worldSlot = -1, bool worldMoon = false)
    {
        if (tower == null) { Hide(); return; }
        if (selected != tower)
        {
            selected = tower;
            title.text = "QUICK BUILD  /  " + tower.name.ToUpperInvariant() + "  /  $" + tower.cost;
            ShowResult("HOVER TO PREVIEW  /  CLICK A NUMBER TO BUILD");
        }
        panel.gameObject.SetActive(true);
        for (int i = 0; i < planetButtons.Length; i++) planetButtons[i].interactable = defense.TowerIn(i) == null;
        for (int i = 0; i < moonButtons.Length; i++) moonButtons[i].interactable = moon.TowerIn(i) == null;
        int slot = worldSlot >= 0 ? worldSlot : hoveredSlot;
        bool onMoon = worldSlot >= 0 ? worldMoon : hoveredMoon;
        if (slot < 0) { preview.Hide(); return; }
        bool occupied = onMoon ? moon.TowerIn(slot) != null : defense.TowerIn(slot) != null;
        if (occupied) { preview.Hide(); return; }
        Vector3 position = onMoon
            ? (Vector3)(moon.Center + (Vector2)PlanetMath.ToLocal(moon.SlotAngle(slot), moon.towerDistance, moon.MoonRadius))
            : defense.SlotPosition(slot);
        float rotation = onMoon ? moon.SlotAngle(slot) - 90f : defense.SlotRotation(slot);
        bool affordable = defense.levelManager == null || defense.levelManager.CanAfford(tower.cost);
        preview.Show(tower, position, rotation, affordable);
    }

    public void Hide()
    {
        panel.gameObject.SetActive(false);
        hoveredSlot = -1;
        preview.Hide();
    }

    public void ShowResult(string result) => status.text = result;
    public bool Contains(Transform hit) => hit == panel || hit.IsChildOf(panel);

    private Button[] AddSlots(Transform row, int count, bool onMoon, Action<int, bool> place)
    {
        var buttons = new Button[count];
        for (int i = 0; i < count; i++)
        {
            int slot = i;
            string number = (onMoon ? "M" : "") + (i + 1);
            var frame = Box("Socket " + number, row, new Color32(5, 10, 22, 255));
            var face = Box("Pixel face", frame, onMoon ? new Color32(65, 73, 102, 255) : new Color32(38, 58, 84, 255));
            Stretch(face, 3f, 3f, 3f, 3f);
            face.GetComponent<Image>().raycastTarget = false;
            var edge = Box("Pixel highlight", face, new Color32(99, 143, 175, 255));
            edge.anchorMin = new Vector2(0f, 1f);
            edge.anchorMax = Vector2.one;
            edge.pivot = new Vector2(0.5f, 1f);
            edge.sizeDelta = new Vector2(-6f, 3f);
            edge.GetComponent<Image>().raycastTarget = false;
            buttons[i] = frame.gameObject.AddComponent<Button>();
            buttons[i].targetGraphic = face.GetComponent<Image>();
            var colors = buttons[i].colors;
            colors.highlightedColor = new Color32(168, 225, 255, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(65, 72, 85, 255);
            colors.fadeDuration = 0f;
            buttons[i].colors = colors;
            buttons[i].onClick.AddListener(() => place(slot, onMoon));
            Text label = Label(frame, number, 22, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 3f, 3f, 3f, 3f);
            var events = frame.gameObject.AddComponent<EventTrigger>();
            AddEvent(events, EventTriggerType.PointerEnter, () => Hover(slot, onMoon));
            AddEvent(events, EventTriggerType.PointerExit, () => Leave(slot, onMoon));
            AddEvent(events, EventTriggerType.Select, () => Hover(slot, onMoon));
            AddEvent(events, EventTriggerType.Deselect, () => Leave(slot, onMoon));
        }
        return buttons;
    }

    private void Hover(int slot, bool onMoon) { hoveredSlot = slot; hoveredMoon = onMoon; }
    private void Leave(int slot, bool onMoon)
    {
        if (hoveredSlot == slot && hoveredMoon == onMoon) { hoveredSlot = -1; preview.Hide(); }
    }

    private static void AddEvent(EventTrigger events, EventTriggerType type, Action action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        events.triggers.Add(entry);
    }

    private Text Label(Transform parent, string value, int size, TextAnchor alignment)
    {
        var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(parent, false);
        label.text = value;
        label.font = font;
        label.fontSize = size;
        label.fontStyle = FontStyle.Bold;
        label.alignment = alignment;
        label.color = new Color32(232, 241, 255, 255);
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Box(string name, Transform parent, Color color)
    {
        var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.GetComponent<Image>().color = color;
        return rt;
    }

    private static void Stretch(RectTransform item, float left, float top, float right, float bottom)
    {
        item.anchorMin = Vector2.zero;
        item.anchorMax = Vector2.one;
        item.offsetMin = new Vector2(left, bottom);
        item.offsetMax = new Vector2(-right, -top);
    }
}

// Copies only sprites: the preview never runs tower scripts, physics, pooling or resource events.
internal sealed class TowerPlacementPreview
{
    private readonly Transform parent;
    private Transform root;
    private TowerData tower;
    private SpriteRenderer[] renderers;
    private Color[] colors;

    public TowerPlacementPreview(Transform parent) { this.parent = parent; }

    public void Show(TowerData selected, Vector3 position, float rotation, bool affordable)
    {
        if (selected.prefab == null) { Hide(); return; }
        if (tower != selected)
        {
            if (root != null) { root.gameObject.SetActive(false); UnityEngine.Object.Destroy(root.gameObject); }
            tower = selected;
            root = CopySprites(selected.prefab.transform, parent);
            root.name = "Tower placement ghost";
            renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i].color;
        }
        root.gameObject.SetActive(true);
        root.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, rotation));
        Color tint = affordable ? new Color(0.75f, 1f, 1f, 0.65f) : new Color(1f, 0.5f, 0.4f, 0.65f);
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = colors[i] * tint;
    }

    public void Hide() { if (root != null) root.gameObject.SetActive(false); }

    private static Transform CopySprites(Transform source, Transform parent)
    {
        var copy = new GameObject(source.name).transform;
        copy.SetParent(parent, false);
        copy.localPosition = source.localPosition;
        copy.localRotation = source.localRotation;
        copy.localScale = source.localScale;
        copy.gameObject.SetActive(source.gameObject.activeSelf);
        var sprite = source.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            var renderer = copy.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite.sprite;
            renderer.color = sprite.color;
            renderer.sharedMaterial = sprite.sharedMaterial;
            renderer.flipX = sprite.flipX;
            renderer.flipY = sprite.flipY;
            renderer.sortingLayerID = sprite.sortingLayerID;
            // Seat behind the same socket rims as a real tower, retaining the part order.
            renderer.sortingOrder = sprite.sortingOrder;
            renderer.drawMode = sprite.drawMode;
            renderer.size = sprite.size;
            renderer.spriteSortPoint = sprite.spriteSortPoint;
            renderer.maskInteraction = sprite.maskInteraction;
            renderer.enabled = sprite.enabled;
        }
        foreach (Transform child in source) CopySprites(child, copy);
        return copy;
    }
}
