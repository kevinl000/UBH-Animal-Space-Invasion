using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Explicit initial authoring only. Saved prefabs and scenes are the editable source thereafter.
public static class PlanetPageSetup
{
    public const string Folder = "Assets/UI/Planets/";
    public static readonly string[] SceneNames = { "UnionPrime", "CapenMajor", "DavisNova", "SilvermanLuna", "C3", "CommonsMinor", "BairdReach" };
    public static readonly string[] Names = { "Union Prime", "Capen Major", "Davis Nova", "Silverman Luna", "C3", "Commons Minor", "Baird Reach" };
    private static readonly Color Navy = new Color32(9, 17, 37, 255);
    private static readonly Color Panel = new Color32(24, 38, 61, 255);
    private static readonly Color Cream = new Color32(255, 239, 195, 255);
    private static readonly Color Blue = new Color32(155, 183, 218, 255);

    [MenuItem("Tools/Planet Pages/Create Missing Phase 1 Assets")]
    public static void Create()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        CreateSilhouettes();
        CreateTowers();
        CreatePlanets();
        if (!File.Exists(Folder + "TowerCard.prefab")) CreateCard();
        if (!File.Exists(Folder + "PlanetHUD.prefab")) CreateHud();
        foreach (string name in SceneNames)
        {
            string path = "Assets/Scenes/" + name + ".unity";
            if (!File.Exists(path)) CreateScene(name, path);
        }
        var scenes = EditorBuildSettings.scenes.ToList();
        foreach (string name in SceneNames)
        {
            string path = "Assets/Scenes/" + name + ".unity";
            if (!scenes.Any(scene => scene.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        LevelSelectSetup.UpdatePlanetRoster();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/UnionPrime.unity");
        Debug.Log("PLANET_PAGES_SETUP_OK");
    }

    private static void CreateTowers()
    {
        // README order: name, unlock, HP, cost, fuel/use (Oil Rig: fuel/s), FOV, damage, attack type, description.
        string[][] rows =
        {
            new[] { "Laser", "0", "25", "1", "-1", "120", "2", "Single", "Shoots laser projectiles." },
            new[] { "Bomb", "0", "25", "2", "-1", "120", "1", "AOE", "Shoots bombs." },
            new[] { "Sniper", "0", "20", "2", "-1", "120", "5", "Single", "Lower fire rate. Higher damage." },
            new[] { "Pest Trap", "0", "20", "2", "-2", "120", "1/s", "DOT", "Shoots a sticky substance that slows enemies and deals damage over 3 seconds." },
            new[] { "Radar", "1", "10", "5", "0", "90", "0", "N/A", "Reveals stealth enemies." },
            new[] { "Oil Rig", "1", "15", "5", "+3", "0", "0", "N/A", "Generates fuel." },
            new[] { "Bank", "1", "15", "5", "0", "0", "0", "N/A", "Generates $3/s." },
            new[] { "Laser SMG", "2", "30", "10", "-3", "120", "2", "Single", "Shoots laser projectiles at a faster rate." },
            new[] { "Tesla Coil", "3", "25", "15", "-3", "120", "2", "AOE", "Fires electricity that damages grouped enemies." },
            new[] { "Gamma Radar", "4", "20", "20", "-5", "120", "1", "AOE", "Reveals stealth enemies while damaging all enemies within its range." },
            new[] { "Ballistica", "5", "30", "25", "-6", "120", "20% Enemy HP", "Single", "Deals percent damage of targeted enemy." }
        };
        for (int i = 0; i < rows.Length; i++)
        {
            string path = Folder + "Tower" + i + ".asset";
            if (File.Exists(path)) continue;
            var row = rows[i];
            var tower = ScriptableObject.CreateInstance<TowerPresentation>();
            tower.displayName = row[0]; tower.unlockLevel = int.Parse(row[1]);
            tower.health = row[2]; tower.cost = row[3]; tower.fuelRate = row[4];
            tower.fieldOfView = row[5]; tower.damage = row[6]; tower.attackType = row[7]; tower.description = row[8];
            tower.portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/LevelSelect/UnknownBoss.png");
            AssetDatabase.CreateAsset(tower, path);
        }
    }

    private static void CreatePlanets()
    {
        string[] landmarks = { "Student Union", "Capen", "Davis", "Silverman", "C3", "Commons", "Baird Point" };
        string[] descriptions =
        {
            "A market and guild world at the heart of the campus constellation.",
            "An artificial celestial hub connecting the campus through the stars.",
            "A bright sun cradled inside a technological ring.",
            "A quiet world with a separate moon waiting on its horizon.",
            "Three small worlds sharing one cosmic address.",
            "A tiny world beneath the silhouette of a campus clock tower.",
            "A desolate frontier of ruins, dead flora, and three ancient pillars."
        };
        string[] bosses = { "Healthy Goose + Healthy Skunk", "Mother Goose", "Mother Skunk", "Restraining Buck", "Sneaky Healthy Goose", "Animal Ship", "Endangered Deer" };
        for (int i = 0; i < SceneNames.Length; i++)
        {
            string path = Folder + SceneNames[i] + ".asset";
            if (File.Exists(path)) continue;
            var planet = ScriptableObject.CreateInstance<PlanetPresentation>();
            planet.displayName = Names[i]; planet.sceneName = SceneNames[i]; planet.level = i;
            planet.landmark = landmarks[i]; planet.description = descriptions[i]; planet.bossName = bosses[i];
            string visualPath = Folder + SceneNames[i] + "Visual.prefab";
            if (!File.Exists(visualPath))
            {
                var visual = Rect(SceneNames[i] + "Visual", null, Vector2.zero, new Vector2(900, 600));
                if (i == 2)
                {
                    Body("Sun", visual, "Body", Vector2.zero, 220, new Color32(246, 193, 104, 255));
                    Body("Ring", visual, "Ring", Vector2.zero, 480, Blue);
                }
                else if (i == 4)
                {
                    Body("BodyA", visual, "Body", new Vector2(-155, 95), 220, Blue);
                    Body("BodyB", visual, "Body", new Vector2(155, 95), 220, new Color32(147, 187, 133, 255));
                    Body("BodyC", visual, "Body", new Vector2(0, -170), 220, new Color32(182, 151, 189, 255));
                }
                else
                {
                    int size = i == 5 ? 260 : i == 6 ? 340 : 420;
                    Body("Body", visual, i == 1 ? "Station" : "Body", Vector2.zero, size,
                        i == 6 ? new Color32(118, 91, 143, 255) : Blue);
                    if (i == 3) Body("Moon", visual, "Body", new Vector2(300, 180), 130, Cream);
                }
                PrefabUtility.SaveAsPrefabAsset(visual.gameObject, visualPath);
                Object.DestroyImmediate(visual.gameObject);
            }
            planet.visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            AssetDatabase.CreateAsset(planet, path);
        }
    }

    private static void CreateSilhouettes()
    {
        foreach (string name in new[] { "Body", "Ring", "Station" })
        {
            string path = Folder + name + ".png";
            if (File.Exists(path)) continue;
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - 47.5f, dy = y - 47.5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                bool inside = radius < 46;
                if (name == "Ring") inside &= radius > 37;
                if (name == "Station") inside = Mathf.Abs(dx) < 40 && Mathf.Abs(dy) < 40 && Mathf.Abs(dx) + Mathf.Abs(dy) < 65;
                if (!inside) continue;
                byte shade = (byte)(dx + dy > 10 ? 150 : dx + dy > -22 ? 205 : 255);
                pixels[y * size + x] = new Color32(shade, shade, shade, 255);
            }
            texture.SetPixels32(pixels); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
    }

    private static void Body(string name, Transform parent, string sprite, Vector2 position, int size, Color color)
    {
        var image = Box(name, parent, position, new Vector2(size, size), color);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + sprite + ".png");
        image.preserveAspect = true;
    }

    private static void CreateCard()
    {
        var root = Box("TowerCard", null, Vector2.zero, new Vector2(188, 112), Panel);
        root.raycastTarget = true;
        var card = root.gameObject.AddComponent<TowerCard>();
        card.button = root.gameObject.AddComponent<Button>();
        card.button.targetGraphic = root;
        Feedback(card.button);
        card.selection = Box("Selection", root.transform, new Vector2(-91, 0), new Vector2(4, 112), Cream);
        card.portrait = Box("Portrait", root.transform, new Vector2(-66, 22), new Vector2(32, 32), Color.white);
        card.title = Label("Name", root.transform, "Tower", new Vector2(24, 25), new Vector2(116, 50), 24, Cream);
        card.cost = Label("Cost", root.transform, "", new Vector2(0, -15), new Vector2(164, 26), 22, Cream);
        card.availability = Label("Availability", root.transform, "", new Vector2(0, -40), new Vector2(164, 24), 19, Blue);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "TowerCard.prefab");
        Object.DestroyImmediate(root.gameObject);
    }

    private static void CreateHud()
    {
        var root = Rect("PlanetHUD", null, Vector2.zero, Vector2.zero);
        Stretch(root);
        var page = root.gameObject.AddComponent<PlanetPageController>();
        page.controls = root.gameObject.AddComponent<CanvasGroup>();
        page.confirmSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/confirmation_001.ogg");
        page.backSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/back_001.ogg");
        Stretch(Box("Background", root, Vector2.zero, Vector2.zero, Navy).rectTransform);
        var playArea = Box("PlayArea", root, Vector2.zero, Vector2.zero, Color.clear);
        Stretch(playArea.rectTransform);
        // Sparse, fixed stars leave room for silhouettes and future combat.
        for (int i = 0; i < 32; i++)
        {
            var star = Box("Star" + i, playArea.transform, Vector2.zero,
                new Vector2(i % 4 == 0 ? 4 : 2, i % 4 == 0 ? 4 : 2), new Color32(61, 82, 112, 255));
            star.rectTransform.anchorMin = star.rectTransform.anchorMax =
                new Vector2((i * 211 % 1600 + 36) / 1672f, (i * 137 % 880 + 30) / 941f);
        }
        page.visualMount = Rect("PlanetVisual", playArea.transform, new Vector2(-220, -10), new Vector2(900, 600));
        page.visualMount.localScale = new Vector3(1.2f, 1.2f, 1);
        page.levelLabel = Label("Level", root, "", new Vector2(-632, -40), new Vector2(320, 28), 20, Blue);
        page.planetName = Label("PlanetName", root, "", new Vector2(-246, -40), new Vector2(-1172, 60), 40, Cream);
        Anchor(page.levelLabel.rectTransform, Vector2.one);
        page.levelLabel.alignment = TextAnchor.MiddleRight;
        page.planetName.rectTransform.anchorMin = new Vector2(0, 1);
        page.planetName.rectTransform.anchorMax = Vector2.one;
        page.planetDescription = Label("PlanetDescription", root, "", new Vector2(-220, 48), new Vector2(-504, 56), 25, Blue);
        page.planetDescription.rectTransform.anchorMin = Vector2.zero;
        page.planetDescription.rectTransform.anchorMax = new Vector2(1, 0);
        var stats = Box("ResourceBar", playArea.transform, new Vector2(-378, -84), new Vector2(-820, 40), Color.clear);
        // Fill the main section up to the right-aligned controls, retaining an 18-unit gap.
        stats.rectTransform.anchorMin = new Vector2(0, 1);
        stats.rectTransform.anchorMax = Vector2.one;
        Stretch(Label("Resources", stats.transform, "HP --     MONEY --     FUEL --     TIME --:--     WAVE --", Vector2.zero, Vector2.zero, 23, Cream).rectTransform);
        var back = MakeButton("ReturnButton", root, "< LEVEL SELECT", new Vector2(166, -40), new Vector2(260, 40));
        Anchor((RectTransform)back.transform, new Vector2(0, 1));
        page.returnButton = back;
        UnityEventTools.AddPersistentListener(back.onClick, page.ReturnToLevelSelect);
        var pause = MakeButton("PauseButton", root, "PAUSE", new Vector2(-700, -84), new Vector2(140, 40));
        var speed = MakeButton("SpeedButton", root, "1x / 2x", new Vector2(-542, -84), new Vector2(140, 40));
        pause.interactable = speed.interactable = false;
        Anchor((RectTransform)pause.transform, Vector2.one);
        Anchor((RectTransform)speed.transform, Vector2.one);

        Box("Sidebar", root, new Vector2(580, 0), new Vector2(440, 901), new Color32(15, 27, 48, 255));
        Label("TowerHeading", root, "TOWER ROSTER", new Vector2(580, 411), new Vector2(384, 44), 32, Cream);
        page.availableCount = Label("AvailableCount", root, "", new Vector2(580, 371), new Vector2(384, 30), 22, Blue);
        var viewport = Box("TowerViewport", root, new Vector2(573, 141), new Vector2(392, 404), Navy);
        viewport.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect("TowerGrid", viewport.transform, Vector2.zero, new Vector2(392, 732));
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1);
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(188, 112); grid.spacing = new Vector2(12, 12);
        grid.padding = new RectOffset(2, 2, 0, 0);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        page.towerScroll = viewport.gameObject.AddComponent<ScrollRect>();
        page.towerScroll.viewport = viewport.rectTransform; page.towerScroll.content = content;
        page.towerScroll.horizontal = false; page.towerScroll.movementType = ScrollRect.MovementType.Clamped;
        page.towerScroll.scrollSensitivity = 35;
        var rail = Box("ScrollRail", root, new Vector2(784, 141), new Vector2(12, 404), Panel);
        var thumb = Box("Thumb", rail.transform, Vector2.zero, Vector2.zero, Blue);
        Stretch(thumb.rectTransform);
        var scrollbar = rail.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = thumb.rectTransform; scrollbar.targetGraphic = thumb;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
        rail.raycastTarget = thumb.raycastTarget = true;
        page.towerScroll.verticalScrollbar = scrollbar;
        page.cards = new TowerCard[11];
        for (int i = 0; i < 11; i++)
        {
            var card = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "TowerCard.prefab"), content)).GetComponent<TowerCard>();
            card.name = "TowerCard" + i;
            card.tower = AssetDatabase.LoadAssetAtPath<TowerPresentation>(Folder + "Tower" + i + ".asset");
            card.Refresh(0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(card);
            page.cards[i] = card;
        }
        for (int i = 0; i < page.cards.Length; i++)
        {
            page.cards[i].button.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = i % 2 == 1 ? page.cards[i - 1].button : back,
                selectOnRight = i % 2 == 0 && i + 1 < 11 ? page.cards[i + 1].button : back,
                selectOnUp = i >= 2 ? page.cards[i - 2].button : back,
                selectOnDown = i + 2 < 11 ? page.cards[i + 2].button : back
            };
            PrefabUtility.RecordPrefabInstancePropertyModifications(page.cards[i].button);
        }
        back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = page.cards[0].button,
            selectOnUp = page.cards[10].button, selectOnDown = page.cards[0].button, selectOnLeft = page.cards[10].button };
        Box("DetailsDivider", root, new Vector2(580, -91), new Vector2(384, 2), Blue);
        page.towerPortrait = Box("DetailPortrait", root, new Vector2(416, -136), new Vector2(48, 48), Color.white);
        page.towerName = Label("TowerName", root, "", new Vector2(622, -132), new Vector2(320, 48), 32, Cream);
        page.towerAvailability = Label("TowerAvailability", root, "", new Vector2(580, -181), new Vector2(384, 30), 21, Blue);
        page.towerStats = Label("TowerStats", root, "", new Vector2(580, -250), new Vector2(384, 104), 26, Cream);
        page.towerDescription = Label("TowerDescription", root, "", new Vector2(580, -357), new Vector2(384, 100), 25, Cream);
        Label("InspectionHint", root, "Inspect only / Building arrives later", new Vector2(580, -425), new Vector2(384, 26), 20, Blue);
        // Keep the roster against the right screen edge while space fills every aspect ratio.
        string[] sidebarParts = { "Sidebar", "TowerHeading", "AvailableCount", "TowerViewport", "ScrollRail",
            "DetailsDivider", "DetailPortrait", "TowerName", "TowerAvailability", "TowerStats", "TowerDescription", "InspectionHint" };
        foreach (string part in sidebarParts)
        {
            var rect = (RectTransform)root.Find(part);
            Anchor(rect, new Vector2(1, 0.5f));
            rect.anchoredPosition -= new Vector2(800, 0);
        }
        var sidebar = (RectTransform)root.Find("Sidebar");
        sidebar.anchorMin = new Vector2(1, 0);
        sidebar.anchorMax = Vector2.one;
        sidebar.sizeDelta = new Vector2(440, 0);
        page.planet = AssetDatabase.LoadAssetAtPath<PlanetPresentation>(Folder + "UnionPrime.asset");
        page.RefreshPresentation();
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "PlanetHUD.prefab");
        Object.DestroyImmediate(root.gameObject);
    }

    private static void CreateScene(string name, string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.orthographic = true; camera.backgroundColor = Navy;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0, 0, -10);
        var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672, 941); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var page = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "PlanetHUD.prefab"), canvas.transform)).GetComponent<PlanetPageController>();
        page.planet = AssetDatabase.LoadAssetAtPath<PlanetPresentation>(Folder + name + ".asset");
        PrefabUtility.RecordPrefabInstancePropertyModifications(page);
        PrefabUtility.InstantiatePrefab(page.planet.visualPrefab, page.visualMount);
        // Identity and locks are applied at runtime; keep scene overrides limited to configuration.
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.SaveScene(scene, path);
    }

    private static Button MakeButton(string name, Transform parent, string title, Vector2 position, Vector2 size)
    {
        var image = Box(name, parent, position, size, Panel); image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Label("Label", image.transform, title, Vector2.zero, size - new Vector2(16, 0), 23, Cream).alignment = TextAnchor.MiddleCenter;
        Feedback(button);
        return button;
    }

    private static void Feedback(Button button)
    {
        var colors = button.colors;
        colors.highlightedColor = colors.selectedColor = new Color32(155, 183, 218, 255);
        colors.normalColor = Color.white; colors.disabledColor = new Color32(90, 100, 120, 255);
        colors.fadeDuration = 0; button.colors = colors;
        button.gameObject.AddComponent<MenuButtonFeedback>().selectSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/tick_001.ogg");
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = rect.anchoredPosition = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 anchor)
    {
        rect.anchorMin = rect.anchorMax = anchor;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false; return image;
    }

    private static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PixelOperator-Bold.ttf");
        text.text = value; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
        return text;
    }
}
