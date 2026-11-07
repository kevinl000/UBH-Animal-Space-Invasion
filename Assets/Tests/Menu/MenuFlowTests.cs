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
        var levelVictor = GameObject.Find("Victor");
        Assert.IsNull(levelVictor.GetComponent<Animator>());
        var levelBackground = GameObject.Find("Background").GetComponent<Image>();
        Assert.AreSame(levelBackground.defaultMaterial, levelBackground.material);
        Assert.IsNull(levelVictor.transform.Find("RightArm"), "The destination menu should use the complete sprite too.");
        foreach (Transform child in levelVictor.transform.parent)
            Assert.AreNotEqual("VictorRightArm", child.name, "The old rotating arm should be removed.");
        Assert.IsFalse(GameObject.Find("LaunchButton").GetComponent<Button>().IsInteractable());
        var destination = GameObject.Find("Destination3").GetComponent<Button>();
        ClickAtCenter(destination);
        Assert.AreEqual("Davis", GameObject.Find("SelectedDestination").GetComponent<Text>().text);
        var page = Object.FindAnyObjectByType<MenuPageController>();
        Assert.Greater(page.confirmSound.samples, 0, "The confirmation sound must contain decoded audio.");

        var back = GameObject.Find("BackButton").GetComponent<Button>();
        ClickAtCenter(back);
        // An extra click during the sound delay must not initiate another scene transition.
        ClickAtCenter(back);
        yield return WaitForScene("MainMenu");
        Assert.AreEqual(1, Object.FindObjectsByType<MenuPageController>().Length);
        Assert.AreEqual("NewGameButton", EventSystem.current.currentSelectedGameObject.name);
        LogAssert.NoUnexpectedReceived();
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
