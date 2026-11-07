using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class LoadingScreenSetup
{
    private const string PrefabPath = "Assets/Resources/SceneLoadingScreen.prefab";
    private static readonly Color Cream = new Color32(255, 239, 195, 255);
    private static readonly Color Blue = new Color32(176, 200, 244, 255);

    [MenuItem("Tools/Main Menu/Create Missing Loading Screen")]
    public static void CreatePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var root = new GameObject("SceneLoadingScreen", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        canvas.pixelPerfect = true;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672, 941);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var backdrop = MakeImage("InputBlocker", root.transform, Vector2.zero, Vector2.zero,
            new Color32(5, 13, 36, 255));
        backdrop.raycastTarget = true;
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        var artwork = MakeImage("Campus", root.transform, Vector2.zero, new Vector2(1672, 941), Color.white);
        artwork.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MainMenu/MainPage-background.png");
        MakeImage("Shade", artwork.transform, Vector2.zero, new Vector2(1672, 941),
            new Color(0.015f, 0.025f, 0.08f, 0.76f));

        MakeText("GameTitle", root.transform, "GAME OF MASCOT", new Vector2(0, 220),
            new Vector2(1100, 80), 54, Cream);
        var status = MakeText("Status", root.transform, "LOADING...", new Vector2(0, 72),
            new Vector2(800, 80), 64, Cream);

        var frame = MakeImage("ProgressFrame", root.transform, Vector2.zero, new Vector2(632, 56), Color.white);
        frame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Kenney/PanelGold.png");
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 0.5f;
        MakeImage("Track", frame.transform, Vector2.zero, new Vector2(608, 32), new Color32(24, 32, 64, 255));
        var fill = MakeImage("ProgressFill", frame.transform, new Vector2(-300, 0), new Vector2(0, 24),
            new Color32(59, 142, 214, 255));
        fill.rectTransform.pivot = new Vector2(0, 0.5f);
        var highlight = MakeImage("Highlight", fill.transform, Vector2.zero, Vector2.zero,
            new Color32(140, 219, 244, 255));
        highlight.rectTransform.anchorMin = new Vector2(0, 1);
        highlight.rectTransform.anchorMax = Vector2.one;
        highlight.rectTransform.pivot = new Vector2(0.5f, 1);
        highlight.rectTransform.sizeDelta = new Vector2(0, 4);
        var percent = MakeText("Percentage", root.transform, "0%", new Vector2(0, -62),
            new Vector2(300, 44), 30, Blue);
        MakeText("Hint", root.transform, "Campus defense is on its way.", new Vector2(0, -148),
            new Vector2(950, 44), 28, Blue);
        MakeText("Footer", root.transform, "PLEASE STAND BY, DEFENDER", new Vector2(0, -360),
            new Vector2(900, 40), 22, Cream);

        var screen = root.AddComponent<SceneLoadingScreen>();
        screen.canvasGroup = root.GetComponent<CanvasGroup>();
        screen.progressFill = fill.rectTransform;
        screen.percentage = percent;
        screen.status = status;
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("LOADING_SCREEN_SETUP_OK");
    }

    public static void CapturePreviews()
    {
        Capture(1672, 941);
        Capture(1280, 960);
        Capture(960, 540);
        Debug.Log("LOADING_SCREEN_PREVIEWS_OK");
    }

    private static void Capture(int width, int height)
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        var screen = root.GetComponent<SceneLoadingScreen>();
        screen.progressFill.sizeDelta = new Vector2(348, 24);
        screen.percentage.text = "58%";
        var cameraObject = new GameObject("LoadingPreviewCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(5, 13, 36, 255);
        camera.orthographic = true;
        var target = new RenderTexture(width, height, 24);
        target.Create();
        camera.targetTexture = target;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = Mathf.Min(width / 1672f, height / 941f);
        canvas.scaleFactor = scaler.scaleFactor;
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        pixels.Apply();
        Directory.CreateDirectory(".git/menu-work");
        File.WriteAllBytes(".git/menu-work/Loading-" + width + "x" + height + ".png", pixels.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        target.Release();
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(pixels);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(cameraObject);
    }

    private static Image MakeImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var image = MakeRect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text MakeText(string name, Transform parent, string value, Vector2 position,
        Vector2 size, int fontSize, Color color)
    {
        var text = MakeRect(name, parent, position, size).gameObject.AddComponent<Text>();
        text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PixelifySans.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
