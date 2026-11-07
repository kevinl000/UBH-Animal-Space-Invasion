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

public class MenuFlowTests
{
    private InputSettings.BackgroundBehavior previousBackgroundBehavior;
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;

    [SetUp]
    public void AllowInputInBatchMode()
    {
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    }

    [TearDown]
    public void RestoreInputSettings()
    {
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
    }

    [UnityTest]
    public IEnumerator MainMenuSupportsKeyboardSelectionMouseNavigationAndReturn()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();

        var newGame = GameObject.Find("NewGameButton").GetComponent<Button>();
        Assert.AreEqual(newGame.gameObject, EventSystem.current.currentSelectedGameObject);
        Assert.IsFalse(GameObject.Find("ContinueButton").GetComponent<Button>().IsInteractable());
        var title = GameObject.Find("GameTitle").GetComponent<Text>();
        Assert.Greater(title.cachedTextGenerator.characterCountVisible, 0, "The title font did not render.");

        var ufo = GameObject.Find("CanadaGooseUfo").GetComponent<Image>();
        Assert.AreEqual(new Vector2(224, 166), ufo.sprite.rect.size, "The UFO must exclude stray extraction pixels.");
        var oldPosition = ufo.rectTransform.anchoredPosition;
        yield return new WaitForSecondsRealtime(0.4f);
        Assert.AreNotEqual(oldPosition, ufo.rectTransform.anchoredPosition, "The UFO should be animated.");

        var victor = GameObject.Find("Victor").GetComponent<Image>();
        Assert.IsNull(victor.transform.Find("RightArm"), "Victor should be one complete static sprite.");
        var animator = victor.GetComponent<Animator>();
        Assert.IsNull(animator, "Victor should stay in a still pose.");
        Assert.IsFalse(victor.raycastTarget);
        var bodyPosition = victor.rectTransform.anchoredPosition;
        var bodySprite = victor.sprite;
        var background = GameObject.Find("Background").GetComponent<Image>();
        Assert.IsFalse(background.raycastTarget);
        Assert.AreSame(background.defaultMaterial, background.material, "The background should use the static UI material.");
        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            float deadline = Time.realtimeSinceStartup + 2;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                Assert.AreEqual(bodyPosition, victor.rectTransform.anchoredPosition);
                Assert.AreSame(bodySprite, victor.sprite, "Victor's body must remain still.");
            }
        }
        finally
        {
            Time.timeScale = previousTimeScale;
        }

        var keyboard = InputSystem.AddDevice<Keyboard>("MenuTestKeyboard");
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return WaitForScene("LevelSelect");
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
        }

        Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>().Length);
        Assert.IsNull(GameObject.Find("Victor"));
        Assert.IsNull(GameObject.Find("CanadaGooseUfo"));
        Assert.IsNull(GameObject.Find("LaunchButton"));
        string[] names = { "Union Prime", "Capen Major", "Davis Nova", "Silverman Luna", "C3", "Commons Minor", "Baird Reach" };
        var confirmed = GameObject.Find("SelectedDestination").GetComponent<Text>();
        AssertConfirmedPlanet(0);
        for (int i = 0; i < names.Length; i++)
        {
            var destination = GameObject.Find("Destination" + i).GetComponent<Button>();
            Assert.AreEqual(names[i], destination.transform.Find("Name").GetComponent<Text>().text);
            Assert.AreEqual(i == 0 ? "TUTORIAL" : "LEVEL " + i,
                destination.transform.Find("Number").GetComponent<Text>().text);
            var planet = destination.transform.Find("Planet").GetComponent<Image>();
            Assert.AreEqual(i == 1 ? 92 : 80, planet.rectTransform.rect.width);
            Assert.IsFalse(planet.raycastTarget);
            ClickAtCenter(destination);
            Assert.AreEqual(names[i], confirmed.text);
            Assert.AreEqual(destination.gameObject, EventSystem.current.currentSelectedGameObject);
            Assert.Greater(destination.targetGraphic.canvasRenderer.GetColor().a, 0,
                "The focused planet needs visible selection brackets.");
            Assert.Greater(destination.targetGraphic.color.a, 0);
            AssertConfirmedPlanet(i);
            var next = GameObject.Find("Destination" + (i + 1) % names.Length);
            ExecuteEvents.Execute(next, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            AssertConfirmedPlanet(i);
            Assert.AreEqual(names[i], confirmed.text, "Hovering another planet must preserve confirmation.");
        }
        var page = Object.FindAnyObjectByType<MenuPageController>();
        Assert.Greater(page.confirmSound.samples, 0, "The confirmation sound must contain decoded audio.");

        var back = GameObject.Find("BackButton").GetComponent<Button>();
        ClickAtCenter(back);
        page.SelectDestination(names[0]);
        Assert.AreEqual(names[6], confirmed.text, "Selection changes must be ignored during navigation.");
        AssertConfirmedPlanet(6);
        // An extra click during loading must not initiate another scene transition.
        ClickAtCenter(back);
        yield return WaitForScene("MainMenu");
        Assert.AreEqual(1, Object.FindObjectsByType<MenuPageController>().Length);
        Assert.AreEqual("NewGameButton", EventSystem.current.currentSelectedGameObject.name);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator LevelSelectDirectEntrySupportsKeyboardSelectionAndEscape()
    {
        yield return SceneManager.LoadSceneAsync("LevelSelect");
        yield return null;
        yield return null;
        Assert.AreEqual("Destination0", EventSystem.current.currentSelectedGameObject.name);
        AssertConfirmedPlanet(0);
        var confirmed = GameObject.Find("SelectedDestination").GetComponent<Text>();
        Assert.IsTrue(GameObject.Find("PlayButton").GetComponent<Button>().IsInteractable());
        var keyboard = InputSystem.AddDevice<Keyboard>("LevelSelectTestKeyboard");
        try
        {
            for (int i = 1; i <= 8; i++)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.AreEqual(i == 8 ? "BackButton" : i == 7 ? "PlayButton" : "Destination" + i,
                    EventSystem.current.currentSelectedGameObject.name);
                Assert.AreEqual("Union Prime", confirmed.text, "Focus alone must not confirm a destination.");
                AssertConfirmedPlanet(0);
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.AreEqual("Destination0", EventSystem.current.currentSelectedGameObject.name);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.AreEqual("Capen Major", confirmed.text);
            AssertConfirmedPlanet(1);
            Assert.AreEqual(GameManager.GameState.Menu, GameManager.Instance.CurrentState);
            Assert.IsFalse(SceneLoadingScreen.IsLoading);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return WaitForScene("MainMenu");
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
        }
        LogAssert.NoUnexpectedReceived();
    }

    private static void AssertConfirmedPlanet(int selectedIndex)
    {
        var page = Object.FindAnyObjectByType<MenuPageController>();
        var details = page.destinationDetails;
        Assert.IsNotNull(details);
        string[] landmarks = { "Student Union", "Capen", "Davis", "Silverman", "C3", "Commons", "Baird Point" };
        Assert.AreEqual(landmarks[selectedIndex] + (selectedIndex == 0 ? " / Tutorial" : " / Level " + selectedIndex),
            details.landmarkAndLevel.text);
        Assert.IsNotEmpty(details.description.text);
        Assert.AreEqual(details.destinations[selectedIndex].description, details.description.text,
            "The details must follow confirmed selection, not focus.");
        Assert.LessOrEqual(details.description.preferredHeight, details.description.rectTransform.rect.height,
            "Every draft description must fit without truncation.");
        Assert.AreEqual(details.destinations[selectedIndex].planet.bossName, details.bossName.text);
        Assert.AreSame(details.unknownBoss, details.bossPreview.sprite);
        Assert.IsFalse(details.bossPreview.raycastTarget);
        Assert.IsTrue(details.playButton.IsInteractable());
        Assert.AreEqual(1, details.playButton.onClick.GetPersistentEventCount());
        for (int i = 0; i < 7; i++)
        {
            var destination = GameObject.Find("Destination" + i);
            var halo = destination.transform.Find("SelectionHalo").GetComponent<Image>();
            Assert.AreEqual(i == selectedIndex, halo.enabled, "Exactly the confirmed planet should illuminate.");
            Assert.IsFalse(halo.raycastTarget, "The halo must not intercept planet clicks.");
            Assert.AreEqual(i == selectedIndex ? page.selectedLabelColor : page.destinationLabelColor,
                destination.transform.Find("Name").GetComponent<Text>().color);
        }
    }

    private static void ClickAtCenter(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.position),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.IsTrue(hits.Count > 0 && hits[0].gameObject == button.gameObject,
            "The visible button should receive the UI raycast.");
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static IEnumerator WaitForScene(string expected)
    {
        float deadline = Time.realtimeSinceStartup + 10;
        while ((SceneManager.GetActiveScene().name != expected || SceneLoadingScreen.IsLoading)
            && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.AreEqual(expected, SceneManager.GetActiveScene().name);
        Assert.IsFalse(SceneLoadingScreen.IsLoading, "The loading overlay should finish before menu input resumes.");
        yield return null;
        yield return null;
    }

}
