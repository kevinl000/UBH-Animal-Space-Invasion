using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSetup
{
    private const string MainScene = "Assets/Scenes/MainMenu.unity";
    private const string LevelsScene = "Assets/Scenes/LevelSelect.unity";
    private const string ArtFolder = "Assets/Art/MainMenu/";
    private static readonly Vector2 ReferenceSize = new Vector2(1672, 941);
    private static readonly Color Cream = new Color32(255, 239, 195, 255);
    private static readonly Color Ink = new Color32(24, 32, 64, 255);

    // Intended for initial setup only. Editing the saved scenes preserves later hand-tuning.
    [MenuItem("Tools/Main Menu/Create Missing Menu Scenes")]
    public static void CreateScenes()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AssetDatabase.Refresh();
        ImportArt();
        if (!File.Exists(MainScene))
            CreateMainPage();
        if (!File.Exists(LevelsScene))
            CreateLevelsPage();

        var otherScenes = EditorBuildSettings.scenes
            .Where(scene => scene.path != MainScene && scene.path != LevelsScene);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScene, true),
            new EditorBuildSettingsScene(LevelsScene, true)
        }.Concat(otherScenes).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MainScene);
        Debug.Log("MAIN_MENU_SETUP_OK: MainMenu and LevelSelect are ready.");
    }

    private static void ImportArt()
    {
        ImportSprite(ArtFolder + "MainPage-background.png", Vector4.zero);
        ImportSprite(ArtFolder + "MainPage-ufo.png", Vector4.zero, new SpriteRect
        {
            name = "CanadaGooseUfo", spriteID = new GUID("657e3619d7b9496f8d2cd5f40f21a8ea"),
            rect = new Rect(1308, 681, 224, 166),
            alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f)
        });
        ImportSprite(ArtFolder + "MainPage-victor-body.png", Vector4.zero, new SpriteRect
        {
            name = "VictorBody", spriteID = new GUID("9201ee554c3a4e0ab044cd2c159e782f"),
            rect = new Rect(1450, 47, 188, 296),
            alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f)
        });
        ImportSprite("Assets/UI/Kenney/PanelGold.png", new Vector4(4, 4, 4, 4));
    }

    private static void ImportSprite(string path, Vector4 border, params SpriteRect[] crops)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = crops.Length > 0 ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = border;
        importer.isReadable = false;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        if (crops.Length > 0)
        {
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(crops);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                crops.Select(crop => new SpriteNameFileIdPair(crop.name, crop.spriteID)));
            provider.Apply();
        }
        importer.SaveAndReimport();
    }

    private static void CreateVictor(RectTransform frame)
    {
        var body = CreateImage("Victor", frame, LoadSprite(ArtFolder + "MainPage-victor-body.png"),
            new Vector2(733, -297.5f), new Vector2(131.6f, 207.2f));
        body.raycastTarget = false;
        body.transform.SetSiblingIndex(frame.Find("Background").GetSiblingIndex() + 1);
    }

    private static void CreateMainPage()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var page = CreatePage(false, out RectTransform content);
        CreateText("Eyebrow", content, "A LITTLE CAMPUS. A BIG GOOSE PROBLEM.",
            new Vector2(0, 285), new Vector2(1000, 48), 29, new Color32(176, 200, 244, 255));
        var title = CreateText("GameTitle", content, "GAME OF MASCOT",
            new Vector2(0, 195), new Vector2(1120, 155), 112, Cream);
        AddShadow(title, new Vector2(5, -6));

        var newGame = CreateButton("NewGameButton", content, "NEW GAME", new Vector2(0, 20),
            new Vector2(360, 84), page);
        UnityEventTools.AddPersistentListener(newGame.onClick, page.OpenLevelsPage);
        var continueButton = CreateButton("ContinueButton", content, "CONTINUE", new Vector2(0, -84),
            new Vector2(360, 84), page);
        continueButton.interactable = false;
        continueButton.GetComponentInChildren<Text>().color = new Color32(82, 88, 112, 255);
        CreateText("NoSaveHint", content, "No saved game yet", new Vector2(0, -151),
            new Vector2(450, 34), 23, new Color32(174, 191, 222, 255));
        CreateText("Footer", content, "UB HACKATHON 2026", new Vector2(0, -432),
            new Vector2(500, 30), 22, Cream);
        page.firstButton = newGame;
        EditorSceneManager.SaveScene(scene, MainScene);
    }

    private static void CreateLevelsPage()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var page = CreatePage(true, out RectTransform content);
        var title = CreateText("LevelsTitle", content, "CHOOSE YOUR PLANET",
            new Vector2(0, 298), new Vector2(1050, 100), 64, Cream);
        AddShadow(title, new Vector2(3, -4));
        CreateText("LevelsSubtitle", content, "Every corner of campus needs a defender.",
            new Vector2(0, 230), new Vector2(1000, 45), 28, new Color32(185, 205, 238, 255));

        string[] names = { "Student Union", "Capen", "C3", "Davis", "Silverman", "Lockwood", "Commons", "Baird Point" };
        Button first = null;
        for (int i = 0; i < names.Length; i++)
        {
            Vector2 position = new Vector2((i % 4 - 1.5f) * 247, 120 - i / 4 * 103);
            var button = CreateButton("Destination" + i, content, names[i], position,
                new Vector2(228, 78), page, 24);
            UnityEventTools.AddStringPersistentListener(button.onClick, page.SelectDestination, names[i]);
            if (first == null) first = button;
        }
        page.selectedDestination = CreateText("SelectedDestination", content, names[0],
            new Vector2(0, -100), new Vector2(700, 50), 38, Cream);
        CreateText("LevelAvailability", content, "More adventures coming soon", new Vector2(0, -148),
            new Vector2(700, 40), 25, new Color32(185, 205, 238, 255));
        var launch = CreateButton("LaunchButton", content, "COMING SOON", new Vector2(185, -222),
            new Vector2(300, 72), page, 30);
        launch.interactable = false;
        launch.GetComponentInChildren<Text>().color = new Color32(82, 88, 112, 255);
        var back = CreateButton("BackButton", content, "BACK", new Vector2(-185, -222),
            new Vector2(300, 72), page, 30);
        UnityEventTools.AddPersistentListener(back.onClick, page.OpenMainPage);
        page.firstButton = first;
        EditorSceneManager.SaveScene(scene, LevelsScene);
    }

    private static MenuPageController CreatePage(bool dimBackground, out RectTransform content)
    {
        var cameraObject = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(5, 13, 36, 255);
        camera.cullingMask = 0;
        camera.orthographic = true;

        var canvasObject = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var frame = CreateRect("ArtworkFrame", canvasObject.transform, Vector2.zero, ReferenceSize);

        var background = CreateImage("Background", frame, LoadSprite(ArtFolder + "MainPage-background.png"),
            Vector2.zero, ReferenceSize);
        background.raycastTarget = false;
        var ufo = CreateImage("CanadaGooseUfo", frame, LoadSprite(ArtFolder + "MainPage-ufo.png"),
            new Vector2(584, 293.5f), new Vector2(224, 166));
        ufo.raycastTarget = false;
        ufo.gameObject.AddComponent<MenuUfoMotion>();
        CreateVictor(frame);

        if (dimBackground)
        {
            var shade = CreateImage("BackgroundShade", frame, null, Vector2.zero, ReferenceSize);
            shade.color = new Color(0.015f, 0.025f, 0.08f, 0.6f);
            shade.raycastTarget = false;
        }
        content = CreateRect("Controls", frame, Vector2.zero, ReferenceSize);
        var controller = content.gameObject.AddComponent<MenuPageController>();
        controller.controls = content.gameObject.AddComponent<CanvasGroup>();
        controller.confirmSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/confirmation_001.ogg");
        controller.backSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/back_001.ogg");

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        return controller;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position,
        Vector2 size, MenuPageController page, int fontSize = 40)
    {
        var image = CreateImage(name, parent, LoadSprite("Assets/UI/Kenney/PanelGold.png"), position, size);
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.5f;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color32(228, 214, 185, 255);
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color32(183, 172, 152, 255);
        colors.disabledColor = new Color32(100, 110, 135, 210);
        colors.fadeDuration = 0;
        button.colors = colors;
        CreateText("Label", image.transform, label, new Vector2(0, 2), size - new Vector2(24, 12), fontSize, Ink);
        var feedback = image.gameObject.AddComponent<MenuButtonFeedback>();
        feedback.selectSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Menu/tick_001.ogg");
        return button;
    }

    private static Text CreateText(string name, Transform parent, string value, Vector2 position,
        Vector2 size, int fontSize, Color color)
    {
        var rect = CreateRect(name, parent, position, size);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PixelifySans.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static void AddShadow(Text text, Vector2 distance)
    {
        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(3, 9, 38, 255);
        shadow.effectDistance = distance;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        var rect = CreateRect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
    }
}
