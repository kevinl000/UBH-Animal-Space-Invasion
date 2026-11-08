using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PlanetPageZoomTests
{
    private static readonly string[] Scenes = { "UnionPrime", "CapenMajor", "DavisNova", "SilvermanLuna", "C3", "CommonsMinor", "BairdReach" };
    private InputSettings.BackgroundBehavior background;
    private InputSettings.EditorInputBehaviorInPlayMode editorInput;
    private Mouse mouse;
    private Touchscreen touchscreen;

    [SetUp]
    public void SetUp()
    {
        background = InputSystem.settings.backgroundBehavior;
        editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        mouse = InputSystem.AddDevice<Mouse>("ZoomTestMouse");
        touchscreen = InputSystem.AddDevice<Touchscreen>("ZoomTestTouchscreen");
    }

    [TearDown]
    public void TearDown()
    {
        InputSystem.RemoveDevice(mouse);
        InputSystem.RemoveDevice(touchscreen);
        InputSystem.settings.backgroundBehavior = background;
        InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        Time.timeScale = 1;
    }

    [UnityTest]
    public IEnumerator EveryPageZoomsTogetherAndLeavesStarsAndHudFixed([ValueSource(nameof(Scenes))] string scene)
    {
        yield return Open(scene);
        var zoom = Object.FindAnyObjectByType<PlanetPageZoom>();
        Assert.IsNotNull(zoom, scene);
        var page = zoom.page;
        Assert.AreEqual(zoom.startingZoom, zoom.CurrentZoom);
        var stars = page.visualMount.parent.Find("Star0");
        Vector3 starPosition = stars.position;
        Vector3 starScale = stars.lossyScale;
        Vector3 hudScale = page.towerScroll.transform.lossyScale;
        Vector3 center = page.visualMount.position;
        var images = page.visualMount.GetComponentsInChildren<Image>();
        Assert.IsNotEmpty(images);
        var imageScales = new Vector3[images.Length];
        for (int i = 0; i < images.Length; i++) imageScales[i] = images[i].transform.lossyScale;

        yield return Scroll(ScreenPoint(zoom.viewport), 1);
        Assert.Greater(zoom.TargetZoom, zoom.startingZoom);
        yield return Scroll(ScreenPoint(zoom.viewport), -1000);
        Assert.AreEqual(zoom.minimumZoom, zoom.TargetZoom);
        yield return Settle(zoom);
        for (int i = 0; i < images.Length; i++)
            Assert.AreEqual(imageScales[i].x * zoom.minimumZoom / zoom.startingZoom, images[i].transform.lossyScale.x, 0.001f);
        Assert.AreEqual(center, page.visualMount.position);
        Assert.AreEqual(starPosition, stars.position);
        Assert.AreEqual(starScale, stars.lossyScale);
        Assert.AreEqual(hudScale, page.towerScroll.transform.lossyScale);

        yield return Scroll(ScreenPoint(zoom.viewport), 1000);
        Assert.AreEqual(zoom.maximumZoom, zoom.TargetZoom);
        yield return Settle(zoom);
        Assert.AreEqual(zoom.maximumZoom, zoom.CurrentZoom);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator RosterScrollAndDisabledControlsDoNotZoomButPausedTimeStillCan()
    {
        yield return Open("UnionPrime");
        var zoom = Object.FindAnyObjectByType<PlanetPageZoom>();
        var page = zoom.page;
        yield return Scroll(ScreenPoint(page.towerScroll.viewport), -5);
        Assert.AreEqual(zoom.startingZoom, zoom.TargetZoom);
        Assert.Greater(page.towerScroll.content.anchoredPosition.y, 0);
        page.controls.interactable = false;
        yield return Scroll(ScreenPoint(zoom.viewport), 5);
        Assert.AreEqual(zoom.startingZoom, zoom.TargetZoom);
        page.controls.interactable = true;
        Time.timeScale = 0;
        yield return Scroll(ScreenPoint(zoom.viewport), 1000);
        yield return Settle(zoom);
        Assert.AreEqual(zoom.maximumZoom, zoom.CurrentZoom);
        Time.timeScale = 1;

        page.ReturnToLevelSelect();
        Assert.IsTrue(SceneLoadingScreen.IsLoading);
        float target = zoom.TargetZoom;
        yield return Scroll(ScreenPoint(zoom.viewport), -1000);
        if (zoom != null) Assert.AreEqual(target, zoom.TargetZoom);
        float deadline = Time.realtimeSinceStartup + 15;
        while (SceneLoadingScreen.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(SceneLoadingScreen.IsLoading);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PinchTracksTwoFingersClampsAndResetsAfterCancellation()
    {
        yield return Open("DavisNova");
        var zoom = Object.FindAnyObjectByType<PlanetPageZoom>();
        Vector2 center = ScreenPoint(zoom.viewport);
        float distance = zoom.viewport.rect.width * zoom.viewport.lossyScale.x * 0.1f;
        Vector2 left = center - Vector2.right * distance;
        Vector2 right = center + Vector2.right * distance;
        yield return TouchPair(1, left, 2, right, UnityEngine.InputSystem.TouchPhase.Began);
        Assert.AreEqual(zoom.startingZoom, zoom.CurrentZoom);
        yield return TouchPair(1, left - Vector2.right * distance, 2, right + Vector2.right * distance, UnityEngine.InputSystem.TouchPhase.Moved);
        Assert.AreEqual(zoom.maximumZoom, zoom.CurrentZoom);
        yield return TouchPair(1, center - Vector2.right * distance * 0.2f, 2, center + Vector2.right * distance * 0.2f, UnityEngine.InputSystem.TouchPhase.Moved);
        Assert.AreEqual(zoom.minimumZoom, zoom.CurrentZoom);
        yield return TouchPair(1, left, 2, right, UnityEngine.InputSystem.TouchPhase.Canceled);
        zoom.SetZoom(zoom.startingZoom, true);
        yield return TouchPair(3, left, 4, right, UnityEngine.InputSystem.TouchPhase.Began);
        Assert.AreEqual(zoom.startingZoom, zoom.CurrentZoom, "A replacement pair starts without a jump.");
        yield return TouchPair(3, left, 4, right, UnityEngine.InputSystem.TouchPhase.Ended);

        Vector2 sidebar = ScreenPoint(zoom.page.towerScroll.viewport);
        yield return TouchPair(5, center, 6, sidebar, UnityEngine.InputSystem.TouchPhase.Began);
        yield return TouchPair(5, left, 6, right, UnityEngine.InputSystem.TouchPhase.Moved);
        Assert.AreEqual(zoom.startingZoom, zoom.CurrentZoom, "A finger starting on the HUD cannot join a pinch.");
        yield return TouchPair(5, left, 6, right, UnityEngine.InputSystem.TouchPhase.Ended);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator SpawnBorderIsIndependentOfZoomAndTracksViewportSize()
    {
        yield return Open("CapenMajor");
        var zoom = Object.FindAnyObjectByType<PlanetPageZoom>();
        Rect bounds = zoom.MaximumViewBounds;
        Assert.IsTrue(bounds.Contains(Vector2.zero));
        var point = zoom.GetSpawnPosition(35, 50);
        zoom.SetZoom(zoom.maximumZoom, true);
        Assert.AreEqual(bounds, zoom.MaximumViewBounds);
        Assert.AreEqual(point, zoom.GetSpawnPosition(35, 50));
        zoom.SetZoom(zoom.minimumZoom, true);
        Assert.AreEqual(bounds, zoom.MaximumViewBounds);
        var corners = new Vector3[4];
        zoom.spawnViewport.GetWorldCorners(corners);
        Vector3 min = zoom.page.visualMount.TransformPoint(bounds.min);
        Vector3 max = zoom.page.visualMount.TransformPoint(bounds.max);
        Assert.Less(Vector3.Distance(corners[0], min), 0.01f);
        Assert.Less(Vector3.Distance(corners[2], max), 0.01f);
        for (int angle = 0; angle < 360; angle += 5)
        {
            Vector2 spawn = zoom.GetSpawnPosition(angle, 50);
            Assert.IsFalse(bounds.Contains(spawn));
            Assert.GreaterOrEqual(Vector2.Distance(spawn, new Vector2(
                Mathf.Clamp(spawn.x, bounds.xMin, bounds.xMax), Mathf.Clamp(spawn.y, bounds.yMin, bounds.yMax))), 50);
        }
        zoom.spawnViewport.sizeDelta += new Vector2(200, 100);
        Canvas.ForceUpdateCanvases();
        Assert.Greater(zoom.MaximumViewBounds.width, bounds.width);
        Assert.Greater(zoom.MaximumViewBounds.height, bounds.height);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator ZoomedOutPointerStillFindsTowerSockets()
    {
        foreach (string scene in new[] { "UnionPrime", "CapenMajor", "DavisNova" })
        {
            yield return Open(scene);
            var zoom = Object.FindAnyObjectByType<PlanetPageZoom>();
            var visual = Object.FindAnyObjectByType<UnionPrimeVisualTest>();
            zoom.SetZoom(zoom.minimumZoom, true);
            for (int i = 0; i < visual.SiteCount; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, visual.ArtRoot.TransformPoint(visual.LocalMount(i)));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                yield return null;
                Assert.IsTrue(visual.IsOccupied(i), scene + " socket " + i);
            }
        }
        LogAssert.NoUnexpectedReceived();
    }

    private static IEnumerator Open(string scene)
    {
        yield return SceneManager.LoadSceneAsync(scene);
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator Scroll(Vector2 position, float delta)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position, scroll = Vector2.up * delta });
        yield return null;
        yield return null;
    }

    private IEnumerator TouchPair(int first, Vector2 firstPosition, int second, Vector2 secondPosition, UnityEngine.InputSystem.TouchPhase phase)
    {
        InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = first, position = firstPosition, phase = phase });
        InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = second, position = secondPosition, phase = phase });
        yield return null;
        yield return null;
    }

    private static IEnumerator Settle(PlanetPageZoom zoom)
    {
        float deadline = Time.realtimeSinceStartup + 3;
        while (zoom.CurrentZoom != zoom.TargetZoom && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(zoom.TargetZoom, zoom.CurrentZoom);
    }

    private static Vector2 ScreenPoint(RectTransform rect)
    {
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }
}
