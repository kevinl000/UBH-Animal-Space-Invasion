using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PlanetPageTests
{
    private static readonly string[] Scenes = { "UnionPrime", "CapenMajor", "DavisNova", "SilvermanLuna", "C3", "CommonsMinor", "BairdReach" };
    private static readonly int[] Counts = { 4, 7, 8, 9, 10, 11, 11 };
    private InputSettings.BackgroundBehavior backgroundBehavior;
    private InputSettings.EditorInputBehaviorInPlayMode editorBehavior;

    [SetUp]
    public void AllowTestInput()
    {
        backgroundBehavior = InputSystem.settings.backgroundBehavior;
        editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    }

    [UnityTearDown]
    public IEnumerator FinishLoading()
    {
        InputSystem.settings.backgroundBehavior = backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
        float deadline = Time.realtimeSinceStartup + 15;
        while (SceneLoadingScreen.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(SceneLoadingScreen.IsLoading);
    }

    [UnityTest]
    public IEnumerator EveryDirectEntryShowsCorrectAvailabilityAndInspectableDetails()
    {
        for (int i = 0; i < Scenes.Length; i++)
        {
            yield return SceneManager.LoadSceneAsync(Scenes[i]);
            yield return null;
            yield return null;
            var page = Object.FindAnyObjectByType<PlanetPageController>();
            Assert.AreEqual(Scenes[i], page.planet.sceneName);
            Assert.AreEqual(page.planet.displayName, page.planetName.text);
            Assert.AreEqual(Counts[i] + " / 11 AVAILABLE", page.availableCount.text);
            Assert.AreEqual(11, page.cards.Length);
            Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>().Length);
            Assert.AreEqual(1, Object.FindObjectsByType<PersistentSystemsController>().Length);
            Assert.IsFalse(GameObject.Find("PauseButton").GetComponent<Button>().IsInteractable());
            Assert.IsFalse(GameObject.Find("SpeedButton").GetComponent<Button>().IsInteractable());
            Assert.IsTrue(GameObject.Find("Resources").GetComponent<Text>().text.Contains("HP --"));
            foreach (var image in page.visualMount.GetComponentsInChildren<Image>())
                Assert.IsFalse(image.raycastTarget);
            int available = 0;
            foreach (var card in page.cards)
            {
                if (card.Available) available++;
                Assert.IsTrue(card.button.IsInteractable(), "Locked towers must remain inspectable.");
                page.RevealCard(card);
                Canvas.ForceUpdateCanvases();
                Click(card.button);
                Assert.AreSame(card, page.SelectedCard);
                Assert.AreEqual(card.tower.displayName, page.towerName.text);
                Assert.AreEqual(card.tower.description, page.towerDescription.text);
                Assert.AreEqual(card.Available ? "AVAILABLE AT THIS DESTINATION" : "LOCKED / UNLOCKS AT LEVEL " + card.tower.unlockLevel,
                    page.towerAvailability.text);
                Assert.That(page.towerStats.text, Does.Contain(card.tower.damage));
                Assert.That(page.towerStats.text, Does.Contain(card.tower.fuelRate + " F/s"));
                Assert.LessOrEqual(page.towerDescription.preferredHeight, page.towerDescription.rectTransform.rect.height);
                Assert.LessOrEqual(page.towerStats.preferredHeight, page.towerStats.rectTransform.rect.height);
                foreach (var other in page.cards) Assert.AreEqual(other == card, other.selection.enabled);
            }
            Assert.AreEqual(Counts[i], available);
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator EveryDestinationLaunchesAndReturnsWithOverlappingInputBlocked()
    {
        for (int i = 0; i < Scenes.Length; i++)
        {
            yield return SceneManager.LoadSceneAsync("LevelSelect");
            yield return null;
            yield return null;
            var menu = Object.FindAnyObjectByType<MenuPageController>();
            Click(GameObject.Find("Destination" + i).GetComponent<Button>());
            Click(menu.destinationDetails.playButton);
            Assert.IsTrue(SceneLoadingScreen.IsLoading);
            menu.LaunchSelectedPlanet();
            menu.OpenMainPage();
            Assert.AreEqual(Scenes[i], GameManager.Instance.LastPlayedLevel);
            yield return WaitForScene(Scenes[i]);
            var page = Object.FindAnyObjectByType<PlanetPageController>();
            Assert.AreEqual(Counts[i] + " / 11 AVAILABLE", page.availableCount.text);
            Assert.AreEqual(GameManager.GameState.Playing, GameManager.Instance.CurrentState);
            var selected = page.SelectedCard;
            Click(page.returnButton);
            page.ReturnToLevelSelect();
            page.SelectTower(page.cards[10]);
            Assert.AreSame(selected, page.SelectedCard, "Tower input is ignored while leaving.");
            yield return WaitForScene("LevelSelect");
            Assert.AreEqual(GameManager.GameState.Menu, GameManager.Instance.CurrentState);
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator KeyboardRevealsLockedCardsWithoutConfirmingUntilSubmitAndEscapeReturns()
    {
        yield return SceneManager.LoadSceneAsync("UnionPrime");
        yield return null;
        yield return null;
        var page = Object.FindAnyObjectByType<PlanetPageController>();
        var keyboard = InputSystem.AddDevice<Keyboard>("PlanetPageTestKeyboard");
        try
        {
            for (int i = 0; i < 5; i++) yield return Press(keyboard, Key.DownArrow);
            Assert.AreEqual(page.cards[10].gameObject, EventSystem.current.currentSelectedGameObject);
            Assert.AreSame(page.cards[0], page.SelectedCard, "Keyboard focus alone must not confirm a card.");
            Assert.Greater(page.towerScroll.content.anchoredPosition.y, 0);
            var viewport = page.towerScroll.viewport;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, page.cards[10].transform);
            Assert.GreaterOrEqual(bounds.min.y, viewport.rect.yMin - 1);
            Assert.LessOrEqual(bounds.max.y, viewport.rect.yMax + 1);
            yield return Press(keyboard, Key.Enter);
            Assert.AreSame(page.cards[10], page.SelectedCard);
            Assert.AreEqual("LOCKED / UNLOCKS AT LEVEL 5", page.towerAvailability.text);
            for (int i = 0; i < 5; i++) yield return Press(keyboard, Key.UpArrow);
            Assert.AreEqual(page.cards[0].gameObject, EventSystem.current.currentSelectedGameObject);
            Assert.AreEqual(0, page.towerScroll.content.anchoredPosition.y, 1);
            var scrollEvent = new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -5) };
            ExecuteEvents.Execute(page.towerScroll.gameObject, scrollEvent, ExecuteEvents.scrollHandler);
            yield return null;
            Assert.Greater(page.towerScroll.content.anchoredPosition.y, 0, "Mouse wheel must scroll the roster.");
            yield return Press(keyboard, Key.Escape);
            yield return WaitForScene("LevelSelect");
        }
        finally { InputSystem.RemoveDevice(keyboard); }
        LogAssert.NoUnexpectedReceived();
    }

    private static IEnumerator Press(Keyboard keyboard, Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.IsTrue(hits.Count > 0 && hits[0].gameObject == button.gameObject,
            button.name + " must receive the visible UI raycast.");
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static IEnumerator WaitForScene(string expected)
    {
        float deadline = Time.realtimeSinceStartup + 15;
        while ((SceneManager.GetActiveScene().name != expected || SceneLoadingScreen.IsLoading)
            && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(expected, SceneManager.GetActiveScene().name);
        Assert.IsFalse(SceneLoadingScreen.IsLoading);
        yield return null;
        yield return null;
    }
}
