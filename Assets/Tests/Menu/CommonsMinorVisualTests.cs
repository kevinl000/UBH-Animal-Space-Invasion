using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CommonsMinorVisualTests
{
    [UnityTest]
    public IEnumerator EightSocketsKeepFoundationLayersAndIndependentAim()
    {
        yield return Open();
        var visual = Object.FindAnyObjectByType<UnionPrimeVisualTest>();
        var page = Object.FindAnyObjectByType<PlanetPageController>();
        Assert.AreEqual("CommonsMinor", page.planet.sceneName);
        Assert.AreEqual(8, visual.SiteCount);
        Assert.AreEqual(0, visual.PlacedCount);
        var terrain = visual.transform.Find("Terrain").GetComponent<Image>();
        var fronts = visual.transform.Find("SocketFronts").GetComponent<Image>();
        Assert.Less(terrain.transform.GetSiblingIndex(), visual.preparedTowerLayer.GetSiblingIndex());
        Assert.Greater(fronts.transform.GetSiblingIndex(), visual.preparedTowerLayer.GetSiblingIndex());
        foreach (var image in new[] { terrain, fronts })
        {
            Assert.IsNotNull(image.sprite);
            Assert.AreEqual(FilterMode.Point, image.sprite.texture.filterMode);
            Assert.AreEqual(1, image.sprite.texture.mipmapCount);
            Assert.AreEqual(new Vector2(1254, 1254), image.sprite.rect.size);
            Assert.AreEqual(Color.white, image.color);
            Assert.IsFalse(image.raycastTarget);
        }
        Assert.AreEqual(41.866f, visual.laserBase.rect.width * visual.towerScale
            * visual.preparedTowerLayer.localScale.x, .001f);
        Assert.IsFalse(visual.TryPlace(-1));
        Assert.IsFalse(visual.TryPlace(8));
        page.SelectTower(page.cards[1]);
        Assert.IsFalse(visual.TryPlace(0));
        page.SelectTower(page.cards[0]);
        for (int i = 0; i < visual.SiteCount; i++)
        {
            Assert.IsTrue(visual.TryPlace(i));
            Assert.IsFalse(visual.TryPlace(i));
        }
        Assert.AreEqual(8, visual.PlacedCount);
        for (int i = 0; i < visual.SiteCount; i++)
        {
            var tower = System.Array.Find(visual.ArtRoot.GetComponentsInChildren<RectTransform>(),
                item => item.name == "Laser / site " + visual.sites[i].number);
            Assert.IsNotNull(tower);
            var mount = tower.localPosition;
            var foundation = tower.localRotation;
            foreach (var aim in new[] { -60f, 0f, 60f })
            {
                visual.SetAim(aim);
                Assert.AreEqual(mount, tower.localPosition);
                Assert.Less(Quaternion.Angle(foundation, tower.localRotation), .001f);
                Assert.Less(Quaternion.Angle(Quaternion.Euler(0, 0, -aim),
                    tower.Find("Gun").localRotation), .001f);
            }
        }
        page.controls.interactable = false;
        Assert.IsFalse(visual.RemoveAt(0));
        page.controls.interactable = true;
        Assert.IsTrue(visual.RemoveAt(0));
        Assert.IsTrue(visual.TryPlace(0));
        yield return Open();
        Assert.AreEqual(0, Object.FindAnyObjectByType<UnionPrimeVisualTest>().PlacedCount);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PointerPlacesEverySocketAtAllZoomsAndCapturesTheIntegratedScene()
    {
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var mouse = InputSystem.AddDevice<Mouse>("CommonsSocketMouse");
        try
        {
            yield return Open();
            var visual = Object.FindAnyObjectByType<UnionPrimeVisualTest>();
            var zoom = Object.FindAnyObjectByType<PlanetPageZoom>();
            var canvas = visual.GetComponentInParent<Canvas>();
            var camera = Object.FindAnyObjectByType<Camera>();
            Capture("empty", 1672, 941, canvas, camera);
            foreach (var level in new[] { zoom.minimumZoom, zoom.startingZoom, zoom.maximumZoom })
            {
                zoom.SetZoom(level, true);
                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < visual.SiteCount; i++)
                {
                    var point = RectTransformUtility.WorldToScreenPoint(null,
                        visual.ArtRoot.TransformPoint(visual.LocalMount(i)));
                    Assert.IsTrue(RectTransformUtility.RectangleContainsScreenPoint(zoom.viewport, point),
                        "Socket " + (i+1) + " must remain in the playfield at zoom " + level);
                    yield return Click(mouse, point, MouseButton.Left);
                    Assert.IsTrue(visual.IsOccupied(i), "Click missed socket " + (i+1) + " at zoom " + level);
                }
                Assert.AreEqual(8, visual.PlacedCount);
                if (level == zoom.startingZoom)
                {
                    Capture("occupied", 1672, 941, canvas, camera);
                    foreach (var aim in new[] { -60f, 60f })
                    {
                        visual.SetAim(aim);
                        Capture("aim" + aim, 1672, 941, canvas, camera);
                    }
                    visual.SetAim(0);
                    Capture("occupied", 1280, 720, canvas, camera);
                    Capture("occupied", 1280, 960, canvas, camera);
                    Capture("occupied", 2560, 1080, canvas, camera);
                }
                if (level == zoom.maximumZoom) Capture("maximum-zoom", 1672, 941, canvas, camera);
                for (int i = 0; i < visual.SiteCount; i++)
                {
                    var point = RectTransformUtility.WorldToScreenPoint(null,
                        visual.ArtRoot.TransformPoint(visual.LocalMount(i)));
                    yield return Click(mouse, point, MouseButton.Right);
                    Assert.IsFalse(visual.IsOccupied(i));
                }
                Assert.AreEqual(0, visual.PlacedCount);
            }
            yield return Click(mouse, RectTransformUtility.WorldToScreenPoint(null, visual.ArtRoot.position), MouseButton.Left);
            Assert.AreEqual(0, visual.PlacedCount, "The planet interior is not a socket.");
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        }
    }

    private static IEnumerator Open()
    {
        yield return SceneManager.LoadSceneAsync("CommonsMinor");
        yield return null;
        yield return null;
    }

    private static IEnumerator Click(Mouse mouse, Vector2 point, MouseButton button)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(button));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
    }

    private static void Capture(string name, int width, int height, Canvas canvas, Camera camera)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        var oldMode = canvas.renderMode;
        var oldCamera = canvas.worldCamera;
        var oldScaleMode = scaler.uiScaleMode;
        var oldScale = scaler.scaleFactor;
        var oldDistance = canvas.planeDistance;
        var previous = RenderTexture.active;
        var oldTarget = camera.targetTexture;
        var oldMask = camera.cullingMask;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
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
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            var directory = Path.GetFullPath(".git/menu-work/commons-minor-unity/captures");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + "-" + width + "x" + height + ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = oldTarget;
            camera.cullingMask = oldMask;
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = oldDistance;
            scaler.uiScaleMode = oldScaleMode;
            scaler.scaleFactor = oldScale;
            canvas.scaleFactor = oldScale;
            Canvas.ForceUpdateCanvases();
            target.Release();
            Object.Destroy(target);
            Object.Destroy(pixels);
        }
    }
}
