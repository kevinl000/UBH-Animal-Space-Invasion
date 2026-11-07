using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoadingScreen : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform progressFill;
    public Text percentage;
    public Text status;
    public float fillWidth = 600;

    private static SceneLoadingScreen instance;
    private int displayedPercent = -1;
    private EventSystem blockedEventSystem;
    private bool previousNavigationEvents;

    public static bool IsLoading => instance != null;

    // Use this entry point for menu, gameplay, retry, and return-to-menu transitions.
    public static bool Load(string sceneName)
    {
        if (IsLoading)
            return false;

        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Scene is missing from the build: " + sceneName);
            return false;
        }

        var prefab = Resources.Load<SceneLoadingScreen>("SceneLoadingScreen");
        if (prefab == null)
        {
            Debug.LogError("The SceneLoadingScreen prefab is missing from Resources.");
            return false;
        }

        instance = Instantiate(prefab);
        DontDestroyOnLoad(instance.gameObject);
        instance.StartCoroutine(instance.ChangeScene(sceneName));
        return true;
    }

    private IEnumerator ChangeScene(string sceneName)
    {
        SetProgress(0);
        BlockNavigation();
        // Submit the overlay before scene deserialization can occupy the main thread.
        yield return null;
        yield return null;

        AsyncOperation operation;
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
            Destroy(gameObject);
            yield break;
        }

        if (operation == null)
        {
            Destroy(gameObject);
            yield break;
        }

        operation.allowSceneActivation = false;
        float visibleTime = 0;
        float shownProgress = 0;
        while (operation.progress < 0.9f || visibleTime < 0.45f || shownProgress < 0.95f)
        {
            visibleTime += Time.unscaledDeltaTime;
            // Unity stops at 0.9 until activation; reserve the final 5% for that step.
            float target = Mathf.Clamp01(operation.progress / 0.9f) * 0.95f;
            shownProgress = Mathf.MoveTowards(shownProgress, target, Time.unscaledDeltaTime * 3);
            SetProgress(shownProgress);
            BlockNavigation();
            yield return null;
        }

        status.text = "ARRIVING...";
        operation.allowSceneActivation = true;
        while (!operation.isDone)
        {
            BlockNavigation();
            yield return null;
        }

        SetProgress(1);
        status.text = "READY!";
        BlockNavigation();
        yield return null;
        for (float elapsed = 0; elapsed < 0.15f; elapsed += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = 1 - elapsed / 0.15f;
            BlockNavigation();
            yield return null;
        }
        Destroy(gameObject);
    }

    private void SetProgress(float value)
    {
        int nextPercent = Mathf.FloorToInt(value * 100);
        if (nextPercent == displayedPercent)
            return;

        displayedPercent = nextPercent;
        percentage.text = nextPercent + "%";
        progressFill.sizeDelta = new Vector2(Mathf.Floor(fillWidth * value / 4) * 4,
            progressFill.sizeDelta.y);
    }

    private void BlockNavigation()
    {
        if (blockedEventSystem == EventSystem.current)
            return;

        RestoreNavigation();
        blockedEventSystem = EventSystem.current;
        if (blockedEventSystem != null)
        {
            previousNavigationEvents = blockedEventSystem.sendNavigationEvents;
            blockedEventSystem.sendNavigationEvents = false;
        }
    }

    private void RestoreNavigation()
    {
        if (blockedEventSystem != null)
            blockedEventSystem.sendNavigationEvents = previousNavigationEvents;
    }

    private void OnDestroy()
    {
        RestoreNavigation();
        if (instance == this)
            instance = null;
    }
}
