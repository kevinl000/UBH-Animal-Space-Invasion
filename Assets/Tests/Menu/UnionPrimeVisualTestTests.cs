using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class UnionPrimeVisualTestTests
{
    [UnityTest]
    public IEnumerator EverySiteAllowsFreeLaserPlacementRemovalAndFreshSceneEntry()
    {
        yield return OpenUnionPrime();
        var visual = Object.FindAnyObjectByType<UnionPrimeVisualTest>();
        var page = Object.FindAnyObjectByType<PlanetPageController>();
        Assert.AreEqual(12, visual.SiteCount);
        Assert.AreEqual(0, visual.PlacedCount);
        Assert.IsFalse(visual.TryPlace(-1));
        Assert.IsFalse(visual.TryPlace(12));
        Assert.IsFalse(visual.RemoveAt(-1));
        string originalCost = visual.laser.cost;

        page.SelectTower(page.cards[1]);
        Assert.IsFalse(visual.TryPlace(0), "Other roster cards remain inspectable, not placeable.");
        page.SelectTower(page.cards[0]);
        for (int cycle = 0; cycle < 3; cycle++)
        {
            for (int i = 0; i < visual.SiteCount; i++)
            {
                Assert.IsTrue(visual.TryPlace(i), "Free placement at site index " + i);
                Assert.IsFalse(visual.TryPlace(i), "Do not stack duplicates into an occupied crater.");
            }
            Assert.AreEqual(12, visual.PlacedCount);
            visual.SetAim(-90);
            Assert.AreEqual(-60, visual.AimDegrees);
            visual.SetAim(90);
            Assert.AreEqual(60, visual.AimDegrees);
            visual.SetAim(0);
            page.controls.interactable = false;
            Assert.IsFalse(visual.RemoveAt(0));
            page.controls.interactable = true;
            for (int i = 0; i < visual.SiteCount; i++) Assert.IsTrue(visual.RemoveAt(i));
            Assert.AreEqual(0, visual.PlacedCount);
        }
        Assert.AreEqual(originalCost, visual.laser.cost, "Testing must not rewrite shared balance/display data.");
        Assert.IsTrue(visual.TryPlace(0));
        yield return OpenUnionPrime();
        Assert.AreEqual(0, Object.FindAnyObjectByType<UnionPrimeVisualTest>().PlacedCount);
        yield return SceneManager.LoadSceneAsync("CapenMajor");
        yield return null;
        Assert.IsNull(Object.FindAnyObjectByType<UnionPrimeVisualTest>(), "Sandbox is Union Prime only.");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PointerPlacesAtCraterIgnoresPlanetCenterAndRemovesWithRightClick()
    {
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var mouse = InputSystem.AddDevice<Mouse>("UnionPrimeVisualTestMouse");
        try
        {
            yield return OpenUnionPrime();
            var visual = Object.FindAnyObjectByType<UnionPrimeVisualTest>();
            var page = Object.FindAnyObjectByType<PlanetPageController>();
            Canvas.ForceUpdateCanvases();
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, visual.ArtRoot.position);
            yield return Click(mouse, center, false);
            Assert.AreEqual(0, visual.PlacedCount, "Planet interior is not a build site.");
            for (int i = 0; i < visual.SiteCount; i++)
            {
                Vector2 position = RectTransformUtility.WorldToScreenPoint(null, visual.ArtRoot.TransformPoint(visual.LocalMount(i)));
                yield return Click(mouse, position, false);
                Assert.IsTrue(visual.IsOccupied(i), "Screen coordinates must hit the visible crater at site " + i);
                yield return Click(mouse, position, true);
                Assert.IsFalse(visual.IsOccupied(i));
            }
            yield return Click(mouse, RectTransformUtility.WorldToScreenPoint(null, page.cards[1].transform.position), false);
            Assert.AreSame(page.cards[1], page.SelectedCard);
            Assert.AreEqual(0, visual.PlacedCount, "Roster input cannot build behind the UI.");
            Vector2 first = RectTransformUtility.WorldToScreenPoint(null, visual.ArtRoot.TransformPoint(visual.LocalMount(0)));
            yield return Click(mouse, first, false);
            Assert.AreEqual(0, visual.PlacedCount, "Only the selected Laser is placeable.");
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        }
        LogAssert.NoUnexpectedReceived();
    }

    private static IEnumerator OpenUnionPrime()
    {
        yield return SceneManager.LoadSceneAsync("UnionPrime");
        yield return null;
        yield return null;
    }

    private static IEnumerator Click(Mouse mouse, Vector2 position, bool right)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = (ushort)(right ? 2 : 1) });
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        yield return null;
    }
}
