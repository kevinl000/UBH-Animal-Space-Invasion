using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SceneLoadingScreenTests
{
    [UnityTearDown]
    public IEnumerator FinishPendingTransition()
    {
        float deadline = Time.realtimeSinceStartup + 15;
        while (SceneLoadingScreen.IsLoading && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.IsFalse(SceneLoadingScreen.IsLoading);
    }

    [UnityTest]
    public IEnumerator LoadingSurvivesSceneReplacementWhilePausedAndBlocksDuplicateRequests()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        yield return null;
        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            Assert.IsTrue(SceneLoadingScreen.Load("LevelSelect"));
            Assert.IsTrue(SceneLoadingScreen.IsLoading);
            Assert.IsFalse(SceneLoadingScreen.Load("MainMenu"));
            var screen = Object.FindAnyObjectByType<SceneLoadingScreen>();
            Assert.IsNotNull(screen);
            Assert.AreEqual("0%", screen.percentage.text);
            Assert.IsFalse(EventSystem.current.sendNavigationEvents);

            // Canvas sizing settles on the first frame, before scene loading starts.
            yield return null;
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width / 2f, Screen.height / 2f)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits);
            Assert.AreEqual("InputBlocker", hits[0].gameObject.name);

            int previousPercent = 0;
            bool sawComplete = false;
            float deadline = Time.realtimeSinceStartup + 15;
            while (SceneLoadingScreen.IsLoading && Time.realtimeSinceStartup < deadline)
            {
                Assert.AreEqual(1, Object.FindObjectsByType<SceneLoadingScreen>().Length);
                int percent = int.Parse(screen.percentage.text.TrimEnd('%'));
                Assert.GreaterOrEqual(percent, previousPercent, "Progress must never move backwards.");
                Assert.That(percent, Is.InRange(0, 100));
                if (percent == 100)
                {
                    sawComplete = true;
                    Assert.AreEqual("LevelSelect", SceneManager.GetActiveScene().name,
                        "100% must only appear after the destination is active.");
                }
                previousPercent = percent;
                yield return null;
            }

            Assert.IsFalse(SceneLoadingScreen.IsLoading, "Loading must finish even with timeScale zero.");
            Assert.IsTrue(sawComplete);
            Assert.AreEqual("LevelSelect", SceneManager.GetActiveScene().name);
            Assert.AreEqual(0, Object.FindObjectsByType<SceneLoadingScreen>().Length);
            Assert.IsTrue(EventSystem.current.sendNavigationEvents);
            Assert.AreEqual(0, Time.timeScale, "The loader must preserve the caller's time scale.");

            // A second transition must work after the persistent overlay is destroyed.
            Assert.IsTrue(SceneLoadingScreen.Load("MainMenu"));
            deadline = Time.realtimeSinceStartup + 15;
            while (SceneLoadingScreen.IsLoading && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsFalse(SceneLoadingScreen.IsLoading);
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.IsTrue(EventSystem.current.sendNavigationEvents);
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            Time.timeScale = previousTimeScale;
        }
    }

    [Test]
    public void MissingSceneDoesNotLeaveAnOverlayOrLockNavigation()
    {
        LogAssert.Expect(LogType.Error, "Scene is missing from the build: MissingLoadingTestScene");
        Assert.IsFalse(SceneLoadingScreen.Load("MissingLoadingTestScene"));
        Assert.IsFalse(SceneLoadingScreen.IsLoading);
        Assert.AreEqual(0, Object.FindObjectsByType<SceneLoadingScreen>().Length);
    }
}
