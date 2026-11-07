using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ManagerIntegrationTests
{
    [UnityTearDown]
    public IEnumerator FinishLoading()
    {
        Time.timeScale = 1;
        float deadline = Time.realtimeSinceStartup + 15;
        while (SceneLoadingScreen.IsLoading && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.IsFalse(SceneLoadingScreen.IsLoading);
    }

    [UnityTest]
    public IEnumerator DirectLevelSelectEntryUsesOnePersistentManagerAndKeepsAudioAcrossNavigation()
    {
        yield return SceneManager.LoadSceneAsync("LevelSelect");
        yield return null;

        var game = GameManager.Instance;
        var scenes = SceneController.Instance;
        var audio = AudioManager.Instance;
        Assert.IsNotNull(game);
        Assert.IsNotNull(scenes);
        Assert.IsNotNull(audio);
        Assert.AreEqual(game.gameObject, audio.gameObject);
        Assert.AreEqual(game.gameObject, scenes.gameObject);
        Assert.AreEqual(0, Object.FindAnyObjectByType<MenuPageController>().GetComponents<AudioSource>().Length);

        Object.Instantiate(Resources.Load<GameObject>("PersistentManager"));
        yield return null;
        Assert.AreEqual(1, Object.FindObjectsByType<PersistentSystemsController>().Length);
        Assert.AreSame(game, GameManager.Instance);
        Assert.AreSame(scenes, SceneController.Instance);
        Assert.AreSame(audio, AudioManager.Instance);

        var sources = audio.GetComponents<AudioSource>();
        Assert.AreEqual(2, sources.Length);
        Assert.AreEqual(0.38f, sources[1].volume, 0.001f, "Preserve the existing menu SFX mix.");
        var clip = AudioClip.Create("PersistentAudioTest", 44100 * 5, 1, 44100, false);
        try
        {
            audio.PlayMusic(clip);
            audio.PlaySFX(clip, 0.35f);
            Time.timeScale = 0;
            game.GoToMainMenu();
            Assert.AreEqual(1, Time.timeScale, "GameManager resets time when leaving gameplay.");
            Assert.AreEqual(GameManager.GameState.Menu, game.CurrentState);
            yield return WaitForScene("MainMenu");

            Assert.AreSame(game, GameManager.Instance);
            Assert.AreSame(audio, AudioManager.Instance);
            Assert.AreSame(clip, sources[0].clip);
            Assert.IsTrue(sources[0].loop);
            Assert.IsTrue(sources[0].isPlaying, "Music must survive scene replacement.");
            Assert.IsTrue(sources[1].isPlaying, "SFX must survive scene replacement.");
        }
        finally
        {
            foreach (var source in sources)
                source.Stop();
            Object.Destroy(clip);
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator GameManagerStartsPausesRestartsAndRejectsInvalidOrOverlappingLevelRequests()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu");
        yield return null;
        var game = GameManager.Instance;
        int completionCount = 0;
        string completedScene = null;
        System.Action onComplete = () =>
        {
            completionCount++;
            completedScene = SceneManager.GetActiveScene().name;
        };
        SceneController.OnLoadComplete += onComplete;
        try
        {
            game.StartLevel("SampleScene");
            Assert.AreEqual(GameManager.GameState.Playing, game.CurrentState);
            game.StartLevel("MainMenu");
            Assert.AreEqual("SampleScene", game.LastPlayedLevel, "A rejected request cannot change restart's target.");
            yield return WaitForScene("SampleScene");
            Assert.AreEqual(1, completionCount);
            Assert.AreEqual("SampleScene", completedScene, "Completion must follow scene activation.");

            game.PauseGame();
            Assert.AreEqual(GameManager.GameState.Paused, game.CurrentState);
            Assert.AreEqual(0, Time.timeScale);
            LogAssert.Expect(LogType.Error, "Scene is missing from the build: MissingIntegrationTestScene");
            game.StartLevel("MissingIntegrationTestScene");
            Assert.AreEqual(GameManager.GameState.Paused, game.CurrentState);
            Assert.AreEqual(0, Time.timeScale);
            Assert.AreEqual("SampleScene", game.LastPlayedLevel);
            Assert.IsFalse(SceneController.Instance.IsLoading);

            game.ResumeGame();
            Assert.AreEqual(GameManager.GameState.Playing, game.CurrentState);
            Assert.AreEqual(1, Time.timeScale);
            game.PauseGame();
            game.RestartCurrentLevel();
            Assert.AreEqual(1, Time.timeScale);
            yield return WaitForScene("SampleScene");
            Assert.AreEqual(2, completionCount);

            game.GoToLevelSelect();
            yield return WaitForScene("LevelSelect");
            Assert.AreEqual(GameManager.GameState.Menu, game.CurrentState);
            Assert.AreEqual(3, completionCount);
        }
        finally
        {
            SceneController.OnLoadComplete -= onComplete;
        }
        LogAssert.NoUnexpectedReceived();
    }

    private static IEnumerator WaitForScene(string expected)
    {
        float deadline = Time.realtimeSinceStartup + 15;
        while (SceneLoadingScreen.IsLoading && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.IsFalse(SceneLoadingScreen.IsLoading);
        Assert.AreEqual(expected, SceneManager.GetActiveScene().name);
        yield return null;
    }
}
