using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class C3VisualTests
{
    [UnityTest]
    public IEnumerator EighteenSocketsKeepIndependentPlacementLayersAndAim()
    {
        yield return Open();
        var motion = Object.FindAnyObjectByType<C3VisualMotion>();
        motion.enabled = false;
        var visuals = motion.GetComponentsInChildren<UnionPrimeVisualTest>();
        var page = Object.FindAnyObjectByType<PlanetPageController>();
        Assert.AreEqual(3, visuals.Length);
        Assert.AreEqual(3, motion.planets.Length);
        Assert.AreEqual(180f, motion.orbitRadius, .001f);
        foreach (var visual in visuals)
        {
            Assert.AreEqual(6, visual.SiteCount);
            Assert.AreEqual(3.125f, visual.towerScale);
            Assert.AreEqual(240f / 1254, visual.transform.localScale.x, .00001f);
            Assert.IsFalse(visual.TryPlace(-1));
            Assert.IsFalse(visual.TryPlace(6));
            var body = visual.GetComponent<Image>();
            Assert.AreEqual(FilterMode.Point, body.sprite.texture.filterMode);
            Assert.AreEqual(1, body.sprite.texture.mipmapCount);
            Assert.AreEqual(Color.white, body.color);
            var front = visual.transform.Find("SocketFronts");
            Assert.Greater(front.GetSiblingIndex(), visual.preparedTowerLayer.GetSiblingIndex());
            page.SelectTower(page.cards[1]);
            Assert.IsFalse(visual.TryPlace(0));
            page.SelectTower(page.cards[0]);
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(visual.TryPlace(i));
                Assert.IsFalse(visual.TryPlace(i));
                var tower = visual.ArtRoot.GetChild(i+1);
                var original = tower.localRotation;
                visual.SetAim(60);
                Assert.Less(Quaternion.Angle(original, tower.localRotation), .001f);
                Assert.Less(Quaternion.Angle(Quaternion.Euler(0, 0, -60), tower.Find("Gun").localRotation), .001f);
                visual.SetAim(0);
            }
            Assert.AreEqual(6, visual.PlacedCount);
        }
        Assert.IsTrue(visuals[1].RemoveAt(2));
        Assert.AreEqual(6, visuals[0].PlacedCount);
        Assert.AreEqual(5, visuals[1].PlacedCount);
        Assert.AreEqual(6, visuals[2].PlacedCount);
        yield return Open();
        foreach (var visual in Object.FindObjectsByType<UnionPrimeVisualTest>(FindObjectsSortMode.None))
            Assert.AreEqual(0, visual.PlacedCount);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator OrbitAndSpinCarryTowersTogetherAndKeepClearance()
    {
        yield return Open();
        var motion = Object.FindAnyObjectByType<C3VisualMotion>();
        var planet = motion.planets[0];
        var visual = planet.GetComponent<UnionPrimeVisualTest>();
        Assert.IsTrue(visual.TryPlace(0));
        var tower = visual.ArtRoot.GetChild(1);
        var localMount = tower.localPosition;
        var foundation = tower.localRotation;
        var startPosition = planet.anchoredPosition;
        var startRotation = planet.localRotation;
        yield return new WaitForSeconds(.4f);
        Assert.Greater(Vector2.Distance(startPosition, planet.anchoredPosition), 1);
        Assert.Greater(Quaternion.Angle(startRotation, planet.localRotation), 1);
        Assert.AreEqual(localMount, tower.localPosition);
        Assert.Less(Quaternion.Angle(foundation, tower.localRotation), .001f);
        foreach (var body in motion.planets)
            Assert.AreEqual(motion.orbitRadius, body.anchoredPosition.magnitude, .001f);
        for (int i = 0; i < motion.planets.Length; i++)
        {
            float separation = Vector2.Distance(motion.planets[i].anchoredPosition,
                motion.planets[(i+1)%3].anchoredPosition);
            Assert.AreEqual(motion.orbitRadius * Mathf.Sqrt(3), separation, .001f);
            // The art check bounds each occupied body within 149 display units,
            // including gun poses. Keep a gap even when occupied sides face inward.
            Assert.Greater(separation - 2*149, 13);
        }
        var page = Object.FindAnyObjectByType<PlanetPageController>();
        page.controls.interactable = false;
        startPosition = planet.anchoredPosition;
        yield return null;
        yield return null;
        Assert.AreEqual(startPosition, planet.anchoredPosition);
        page.controls.interactable = true;
        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            startPosition = planet.anchoredPosition;
            yield return null;
            yield return null;
            Assert.AreEqual(startPosition, planet.anchoredPosition);
        }
        finally { Time.timeScale = previousTimeScale; }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PointerPlacesAllEighteenMovingSocketsAndRemovesAtMinimumZoom()
    {
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var mouse = InputSystem.AddDevice<Mouse>("C3SocketMouse");
        try
        {
            yield return Open();
            var zoom = Object.FindAnyObjectByType<PlanetPageZoom>();
            zoom.SetZoom(zoom.minimumZoom, true);
            Canvas.ForceUpdateCanvases();
            var visuals = Object.FindObjectsByType<UnionPrimeVisualTest>(FindObjectsSortMode.None);
            foreach (var visual in visuals)
            {
                for (int i = 0; i < visual.SiteCount; i++)
                {
                    yield return Click(mouse, visual, i, MouseButton.Left);
                    Assert.IsTrue(visual.IsOccupied(i), visual.name + " / socket " + (i+1));
                }
            }
            foreach (var visual in visuals) Assert.AreEqual(6, visual.PlacedCount);
            yield return Click(mouse, visuals[1], 4, MouseButton.Right);
            Assert.AreEqual(5, visuals[1].PlacedCount);
            Assert.AreEqual(6, visuals[0].PlacedCount);
            Assert.AreEqual(6, visuals[2].PlacedCount);
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        }
    }

    private static IEnumerator Click(Mouse mouse, UnionPrimeVisualTest visual, int index, MouseButton button)
    {
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, visual.ArtRoot.TransformPoint(visual.LocalMount(index)));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
        yield return null;
        point = RectTransformUtility.WorldToScreenPoint(null, visual.ArtRoot.TransformPoint(visual.LocalMount(index)));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(button));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
    }

    private static IEnumerator Open()
    {
        yield return SceneManager.LoadSceneAsync("C3");
        yield return null;
        yield return null;
    }
}
