using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LevelSelectSetup
{
    private const string ScenePath = "Assets/Scenes/LevelSelect.unity";
    private static readonly Vector2 ReferenceSize = new Vector2(1672, 941);
    private static readonly string[] Names =
    {
        "Union Prime", "Capen Major", "C3", "Davis Nova", "Silverman Luna",
        "Ellicott Prime", "Lockwood Terra", "Commons Minor", "Baird's Reach"
    };
    private static readonly float[] PlanetHeights = { 8, 60, -12, 40, -20, 52, -8, 28, 0 };

    [MenuItem("Tools/Level Select/Align Sun and Orbits")]
    public static void AlignSunAndOrbits()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        AlignBackground(Object.FindAnyObjectByType<MenuPageController>());
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL_SELECT_ORBITS_OK");
    }

    private static void AlignBackground(MenuPageController page)
    {
        Canvas.ForceUpdateCanvases();
        var frame = page.transform.parent;
        var background = frame.Find("Background").GetComponent<Image>();
        var sun = frame.Find("Sun").GetComponent<RectTransform>();
        var firstPlanet = page.transform.Find("Destination0/Planet");
        sun.anchoredPosition = new Vector2(sun.anchoredPosition.x,
            Mathf.Round(frame.InverseTransformPoint(firstPlanet.position).y / 4f) * 4f);
        var planetPositions = new Vector2[Names.Length];
        for (int i = 0; i < planetPositions.Length; i++)
            planetPositions[i] = background.transform.InverseTransformPoint(page.transform.Find("Destination" + i + "/Planet").position);
        LevelSelectArt.CreateBackground(background.transform.InverseTransformPoint(sun.position), planetPositions);
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LevelSelectArt.Folder + "StarMap.png");
    }

    [MenuItem("Tools/Level Select/Rebalance Page Spacing")]
    public static void RebalanceSpacing()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        ApplyPageSpacing(Object.FindAnyObjectByType<MenuPageController>());
        EditorSceneManager.SaveScene(scene);
        Debug.Log("LEVEL_SELECT_SPACING_OK");
    }

    // Shared by scene creation and the layout-only update; preserve content and button wiring.
    private static void ApplyPageSpacing(MenuPageController page)
    {
        var controls = page.transform;
        controls.Find("Eyebrow").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 424);
        controls.Find("LevelsTitle").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 360);
        controls.Find("LevelsSubtitle").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 292);
        for (int i = 0; i < Names.Length; i++)
            controls.Find("Destination" + i).GetComponent<RectTransform>().anchoredPosition =
                new Vector2(-600 + i * 164, PlanetHeights[i] + 96);

        var panel = page.destinationDetails.GetComponent<RectTransform>();
        panel.anchoredPosition = new Vector2(0, -216);
        panel.sizeDelta = new Vector2(1512, 272);
        page.selectedDestination.rectTransform.anchoredPosition = new Vector2(-340, 92);
        page.destinationDetails.landmarkAndLevel.rectTransform.anchoredPosition = new Vector2(-340, 44);
        page.destinationDetails.description.rectTransform.anchoredPosition = new Vector2(-340, -36);
        page.destinationDetails.description.rectTransform.sizeDelta = new Vector2(748, 104);
        panel.Find("Divider").GetComponent<RectTransform>().sizeDelta = new Vector2(4, 208);
        panel.Find("BossCaption").GetComponent<RectTransform>().anchoredPosition = new Vector2(240, 92);
        page.destinationDetails.bossName.rectTransform.anchoredPosition = new Vector2(240, -92);
        page.destinationDetails.playButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(560, 16);
        panel.Find("PlayAvailability").GetComponent<RectTransform>().anchoredPosition = new Vector2(560, -48);
        controls.Find("BackButton").GetComponent<RectTransform>().anchoredPosition = new Vector2(-648, -412);
        AlignBackground(page);
    }

    [MenuItem("Tools/Level Select/Create Missing Details Panel")]
    public static void CreateDetailsPanel()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var page = Object.FindAnyObjectByType<MenuPageController>();
        if (page.destinationDetails != null) return;
        LevelSelectArt.GenerateDetailsArt();
        SetUpDetailsPanel(page);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL_SELECT_DETAILS_OK");
    }

    private static void SetUpDetailsPanel(MenuPageController page)
    {
        var panel = Image("PlanetDetails", page.transform, "DetailsPanel", new Vector2(0, -280), new Vector2(1512, 224));
        panel.type = UnityEngine.UI.Image.Type.Sliced;
        panel.pixelsPerUnitMultiplier = 0.25f;
        var details = panel.gameObject.AddComponent<LevelSelectDetails>();
        page.destinationDetails = details;
        Object.DestroyImmediate(page.transform.Find("SelectionCaption").gameObject);
        var name = page.selectedDestination;
        name.transform.SetParent(panel.transform, false);
        name.rectTransform.anchoredPosition = new Vector2(-340, 76);
        name.rectTransform.sizeDelta = new Vector2(748, 48);
        name.alignment = TextAnchor.MiddleLeft;
        details.landmarkAndLevel = Text("LandmarkAndLevel", panel.transform, "", new Vector2(-340, 32), new Vector2(748, 32), 20, LevelSelectArt.Blue);
        details.landmarkAndLevel.alignment = TextAnchor.MiddleLeft;
        details.description = Text("Description", panel.transform, "", new Vector2(-340, -38), new Vector2(748, 88), 24, LevelSelectArt.Cream);
        details.description.alignment = TextAnchor.UpperLeft;

        var divider = Image("Divider", panel.transform, null, new Vector2(88, 0), new Vector2(4, 168));
        divider.color = new Color32(42, 61, 86, 255);
        Text("BossCaption", panel.transform, "BOSS", new Vector2(240, 78), new Vector2(200, 28), 18, LevelSelectArt.Blue);
        details.bossPreview = Image("BossPreview", panel.transform, "UnknownBoss", new Vector2(240, 0), new Vector2(96, 96));
        details.bossPreview.preserveAspect = true;
        details.unknownBoss = details.bossPreview.sprite;
        details.bossName = Text("BossName", panel.transform, "Unknown", new Vector2(240, -78), new Vector2(264, 32), 22, LevelSelectArt.Cream);

        var playImage = Image("PlayButton", panel.transform, null, new Vector2(560, 4), new Vector2(216, 64));
        playImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Kenney/PanelGold.png");
        playImage.type = UnityEngine.UI.Image.Type.Sliced;
        playImage.pixelsPerUnitMultiplier = 0.5f;
        playImage.raycastTarget = true;
        Text("Label", playImage.transform, "PLAY", Vector2.zero, new Vector2(196, 52), 28, LevelSelectArt.Blue);
        details.playButton = playImage.gameObject.AddComponent<Button>();
        details.playButton.targetGraphic = playImage;
        var colors = details.playButton.colors;
        colors.disabledColor = new Color32(67, 80, 101, 255);
        details.playButton.colors = colors;
        details.playButton.navigation = new Navigation { mode = Navigation.Mode.None };
        details.playButton.interactable = false;
        Text("PlayAvailability", panel.transform, "Coming soon", new Vector2(560, -54), new Vector2(264, 40), 20, LevelSelectArt.Blue);

        string[] landmarks = { "Student Union", "Capen", "C3", "Davis", "Silverman", "Ellicott", "Lockwood", "Commons", "Baird Point" };
        // Draft flavor only: no difficulty, enemy, or resource promises.
        string[] descriptions =
        {
            "Every campus voyage begins somewhere. Find your bearings at the heart of this little star system.",
            "A familiar campus landmark, reimagined on a grander orbit. Welcome to Capen among the stars.",
            "A short name with a cosmic address. C3 brings a familiar corner of campus into the constellation.",
            "A new light in a familiar sky. Davis takes its place among the worlds of the campus constellation.",
            "Silver light falls across a quiet world. This is Silverman, a little farther from home.",
            "A grand world with a familiar name. Ellicott stretches across the horizon of the campus sky.",
            "A small world with room for big ideas. Lockwood finds a new home beneath unfamiliar stars.",
            "A little meeting place in a very big universe. Familiar paths converge on Commons Minor.",
            "The campus horizon opens into deep space. Baird's Reach looks out toward the stars beyond."
        };
        details.destinations = new LevelSelectDetails.Destination[Names.Length];
        for (int i = 0; i < Names.Length; i++)
            details.destinations[i] = new LevelSelectDetails.Destination
            {
                landmarkAndLevel = landmarks[i] + (i == 0 ? " / Tutorial" : " / Level " + i),
                description = descriptions[i]
            };

        var back = page.transform.Find("BackButton").GetComponent<RectTransform>();
        back.anchoredPosition = new Vector2(-648, -432);
        back.sizeDelta = new Vector2(216, 56);
        ApplyPageSpacing(page);
        page.RefreshDestinationSelection();
    }

    [MenuItem("Tools/Level Select/Update Planet Names and Selection")]
    public static void UpdatePlanetSelection()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        LevelSelectArt.GenerateSelectionHalos();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        SetUpPlanetSelection(Object.FindAnyObjectByType<MenuPageController>());
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL_SELECT_SELECTION_OK");
    }

    private static void SetUpPlanetSelection(MenuPageController page)
    {
        page.destinationHalos = new Image[Names.Length];
        page.destinationLabels = new Text[Names.Length];
        page.destinationLabelColor = LevelSelectArt.Blue;
        page.selectedLabelColor = LevelSelectArt.Cream;
        for (int i = 0; i < Names.Length; i++)
        {
            var button = page.transform.Find("Destination" + i).GetComponent<Button>();
            var label = button.transform.Find("Name").GetComponent<Text>();
            label.text = Names[i];
            UnityEventTools.RegisterStringPersistentListener(button.onClick, 0, page.SelectDestination, Names[i]);
            var existingHalo = button.transform.Find("SelectionHalo");
            bool large = i == 1 || i == 5;
            int diameter = large ? 124 : 112;
            var halo = existingHalo != null ? existingHalo.GetComponent<Image>() :
                Image("SelectionHalo", button.transform, large ? "SelectionHaloLarge" : "SelectionHalo",
                    new Vector2(0, 24), new Vector2(diameter, diameter));
            halo.transform.SetAsFirstSibling();
            page.destinationLabels[i] = label;
            page.destinationHalos[i] = halo;
        }
        page.selectedDestination.text = Names[0];
        page.RefreshDestinationSelection();
    }

    // Explicit rebuild only: opening the project never overwrites scene hand-tuning.
    [MenuItem("Tools/Level Select/Rebuild Background and Layout")]
    public static void Rebuild()
    {
        if (!Application.isBatchMode)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Rebuild LevelSelect",
                "Replace the LevelSelect artwork and controls with the step 1 layout?", "Rebuild", "Cancel")) return;
        }

        LevelSelectArt.Generate();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var oldFrame = canvas.transform.Find("ArtworkFrame");
        Object.DestroyImmediate(oldFrame.gameObject);
        var camera = Object.FindAnyObjectByType<Camera>();
        camera.backgroundColor = LevelSelectArt.Navy;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.referenceResolution = ReferenceSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var frame = Rect("ArtworkFrame", canvas.transform, Vector2.zero, ReferenceSize);
        frame.gameObject.AddComponent<RectMask2D>();
        Image("Background", frame, "StarMap", Vector2.zero, new Vector2(1672, 944));
        Image("Sun", frame, "Sun", new Vector2(-872, 0), new Vector2(384, 384));
        var controls = Rect("Controls", frame, Vector2.zero, ReferenceSize);
        var page = controls.gameObject.AddComponent<MenuPageController>();
        page.controls = controls.gameObject.AddComponent<CanvasGroup>();
        page.confirmSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/confirmation_001.ogg");
        page.backSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/back_001.ogg");

        Text("Eyebrow", controls, "CAMPUS CONSTELLATION", new Vector2(0, 352), new Vector2(900, 36), 22, LevelSelectArt.Blue);
        Text("LevelsTitle", controls, "CHOOSE YOUR PLANET", new Vector2(0, 288), new Vector2(1200, 92), 64, LevelSelectArt.Cream);
        Text("LevelsSubtitle", controls, "Nine destinations. One campus to defend.", new Vector2(0, 220), new Vector2(1000, 44), 26, LevelSelectArt.Blue);

        var buttons = new Button[10];
        for (int i = 0; i < Names.Length; i++)
        {
            var hit = Image("Destination" + i, controls, null, new Vector2(-600 + i * 164, PlanetHeights[i]), new Vector2(152, 196));
            hit.color = Color.clear;
            hit.raycastTarget = true;
            int diameter = i == 1 || i == 5 ? 92 : 80;
            Image("Planet", hit.transform, "Planet" + i, new Vector2(0, 24), new Vector2(diameter, diameter));
            var focus = Image("Focus", hit.transform, "Focus", new Vector2(0, 24), new Vector2(120, 120));
            Text("Name", hit.transform, Names[i], new Vector2(0, -58), new Vector2(156, 58), 24, LevelSelectArt.Cream);
            Text("Number", hit.transform, i == 0 ? "TUTORIAL" : "LEVEL " + i,
                new Vector2(0, -100), new Vector2(152, 30), 18, LevelSelectArt.Blue);
            var button = hit.gameObject.AddComponent<Button>();
            button.targetGraphic = focus;
            var colors = button.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(1, 1, 1, 0.5f);
            colors.fadeDuration = 0;
            button.colors = colors;
            AddFeedback(button);
            UnityEventTools.AddStringPersistentListener(button.onClick, page.SelectDestination, Names[i]);
            buttons[i] = button;
        }

        Text("SelectionCaption", controls, "SELECTED DESTINATION", new Vector2(0, -232), new Vector2(700, 32), 18, LevelSelectArt.Blue);
        page.selectedDestination = Text("SelectedDestination", controls, Names[0], new Vector2(0, -276), new Vector2(800, 56), 36, LevelSelectArt.Cream);
        var backImage = Image("BackButton", controls, null, new Vector2(0, -368), new Vector2(216, 64));
        backImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Kenney/PanelGold.png");
        backImage.type = UnityEngine.UI.Image.Type.Sliced;
        backImage.pixelsPerUnitMultiplier = 0.5f;
        backImage.raycastTarget = true;
        Text("Label", backImage.transform, "BACK", Vector2.zero, new Vector2(196, 52), 28, LevelSelectArt.Navy);
        var back = backImage.gameObject.AddComponent<Button>();
        back.targetGraphic = backImage;
        var backColors = back.colors;
        backColors.normalColor = new Color32(210, 199, 175, 255);
        backColors.selectedColor = backColors.highlightedColor = Color.white;
        backColors.fadeDuration = 0;
        back.colors = backColors;
        AddFeedback(back);
        UnityEventTools.AddPersistentListener(back.onClick, page.OpenMainPage);
        buttons[9] = back;
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = buttons[(i + buttons.Length - 1) % buttons.Length],
                selectOnRight = buttons[(i + 1) % buttons.Length],
                selectOnUp = i == 9 ? buttons[0] : buttons[(i + buttons.Length - 1) % buttons.Length],
                selectOnDown = i == 9 ? buttons[0] : back
            };
        }
        page.firstButton = buttons[0];
        SetUpPlanetSelection(page);
        SetUpDetailsPanel(page);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL_SELECT_SETUP_OK");
    }

    private static void AddFeedback(Button button)
    {
        button.gameObject.AddComponent<MenuButtonFeedback>().selectSound =
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/tick_001.ogg");
    }

    private static Image Image(string name, Transform parent, string sprite, Vector2 position, Vector2 size)
    {
        var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        if (sprite != null) image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LevelSelectArt.Folder + sprite + ".png");
        image.raycastTarget = false;
        return image;
    }

    private static Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PixelifySans.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
