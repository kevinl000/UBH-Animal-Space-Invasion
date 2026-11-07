using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Off-screen scene previews for checking layout without opening a visible editor window.
public static class MenuPreviewCapture
{
    public static void CapturePlanets()
    {
        foreach (string scene in PlanetPageSetup.SceneNames)
            CaptureScene(scene, 1672, 941);
        CaptureScene("UnionPrime", 1280, 720);
        CaptureScene("UnionPrime", 1280, 960);
        CaptureScene("UnionPrime", 2560, 1080);
        CaptureLevelSelect();
        EditorSceneManager.OpenScene("Assets/Scenes/UnionPrime.unity");
        Debug.Log("PLANET_PREVIEWS_OK");
    }

    public static void CaptureLevelSelect()
    {
        CaptureScene("LevelSelect", 1672, 941);
        CaptureScene("LevelSelect", 1280, 720);
        CaptureScene("LevelSelect", 1280, 960);
        CaptureScene("LevelSelect", 2560, 1080);
        EditorSceneManager.OpenScene("Assets/Scenes/LevelSelect.unity");
        Debug.Log("LEVEL_SELECT_PREVIEWS_OK");
    }

    public static void Capture()
    {
        CaptureScene("MainMenu", 1672, 941);
        CaptureScene("LevelSelect", 1672, 941);
        CaptureScene("MainMenu", 1280, 960);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("MENU_PREVIEWS_OK");
    }

    private static void CaptureScene(string sceneName, int width, int height)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
        var planetPage = Object.FindAnyObjectByType<PlanetPageController>();
        if (planetPage != null) planetPage.RefreshPresentation();
        var camera = Object.FindAnyObjectByType<Camera>();
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>();
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        target.Create();
        camera.targetTexture = target;
        camera.cullingMask = ~0;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = Mathf.Min(width / 1672f, height / 941f);
        canvas.scaleFactor = scaler.scaleFactor;
        Canvas.ForceUpdateCanvases();

        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
        RenderPipeline.SubmitRenderRequest(camera, request);
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        pixels.Apply();
        string directory = Path.GetFullPath(".git/menu-work");
        Directory.CreateDirectory(directory);
        string name = sceneName + "-" + width + "x" + height;
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        target.Release();
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(pixels);
        // The capture-only camera/canvas changes are deliberately not saved to the scene.
    }
}
