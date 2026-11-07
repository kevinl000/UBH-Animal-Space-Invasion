using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class UnionPrimeVisualTestTests
{
    private static readonly string[] Scenes = { "UnionPrime", "CapenMajor", "DavisNova" };

    [UnityTest]
    public IEnumerator EverySiteAllowsFreeLaserPlacementRemovalAndFreshSceneEntry([ValueSource(nameof(Scenes))] string scene)
    {
        yield return OpenPlanet(scene);
        var visual = Object.FindAnyObjectByType<UnionPrimeVisualTest>();
        var page = Object.FindAnyObjectByType<PlanetPageController>();
        int expectedSites = scene == "CapenMajor" ? 16 : 12;
        Assert.AreEqual(expectedSites, visual.SiteCount);
        Assert.AreEqual(0, visual.PlacedCount);
        Assert.IsFalse(visual.TryPlace(-1));
        Assert.IsFalse(visual.TryPlace(expectedSites));
        Assert.IsFalse(visual.RemoveAt(-1));
        string originalCost = visual.laser.cost;
        if (visual.preparedTowerLayer != null)
        {
            var front = visual.transform.Find(scene == "DavisNova" ? "SocketFronts" : "SteelFronts");
            Assert.Greater(front.GetSiblingIndex(), visual.preparedTowerLayer.GetSiblingIndex(),
                "Prepared front edges must cover the tower foundations.");
            Assert.AreEqual(scene == "CapenMajor" ? 1.5f : 2f, visual.towerScale);
        }

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
            Assert.AreEqual(expectedSites, visual.PlacedCount);
            var tower = System.Array.Find(visual.ArtRoot.GetComponentsInChildren<UnityEngine.UI.Image>(),
                image => image.name == "Base and operator");
            Assert.IsNotNull(tower);
            var baseRotation = tower.transform.rotation;
            visual.SetAim(-90);
            Assert.AreEqual(-60, visual.AimDegrees);
            visual.SetAim(90);
            Assert.AreEqual(60, visual.AimDegrees);
            Assert.AreEqual(baseRotation, tower.transform.rotation, "Aiming must leave the foundation seated.");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0, 0, -60),
                tower.transform.parent.Find("Gun").localRotation), 0.01f);
            visual.SetAim(0);
            page.controls.interactable = false;
            Assert.IsFalse(visual.RemoveAt(0));
            page.controls.interactable = true;
            for (int i = 0; i < visual.SiteCount; i++) Assert.IsTrue(visual.RemoveAt(i));
            Assert.AreEqual(0, visual.PlacedCount);
        }
        Assert.AreEqual(originalCost, visual.laser.cost, "Testing must not rewrite shared balance/display data.");
        Assert.IsTrue(visual.TryPlace(0));
        yield return OpenPlanet(scene);
        Assert.AreEqual(0, Object.FindAnyObjectByType<UnionPrimeVisualTest>().PlacedCount);
        yield return SceneManager.LoadSceneAsync("SilvermanLuna");
        yield return null;
        Assert.IsNull(Object.FindAnyObjectByType<UnionPrimeVisualTest>(), "Other planets remain unchanged.");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PointerPlacesAtCraterIgnoresPlanetCenterAndRemovesWithRightClick([ValueSource(nameof(Scenes))] string scene)
    {
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var mouse = InputSystem.AddDevice<Mouse>("UnionPrimeVisualTestMouse");
        try
        {
            yield return OpenPlanet(scene);
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

    private static IEnumerator OpenPlanet(string scene)
    {
        yield return SceneManager.LoadSceneAsync(scene);
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
