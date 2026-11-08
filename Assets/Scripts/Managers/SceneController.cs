using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    public static event Action OnLoadBegin;
    public static event Action<float> OnLoadProgress;
    public static event Action OnLoadComplete;

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void LoadScene(string sceneName)
    {
        TryLoadScene(sceneName);
    }

    public bool TryLoadScene(string sceneName)
    {
        if (IsLoading)
            return false;

        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Scene is missing from the build: " + sceneName);
            return false;
        }

        IsLoading = true;
        StartCoroutine(LoadSceneRoutine(sceneName));
        return true;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        OnLoadBegin?.Invoke();
        OnLoadProgress?.Invoke(0f);
        // Let the loading UI render before scene deserialization occupies the main thread.
        yield return null;
        yield return null;

        AsyncOperation asyncLoad;
        try
        {
            asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            IsLoading = false;
            OnLoadComplete?.Invoke();
            yield break;
        }

        if (asyncLoad == null)
        {
            IsLoading = false;
            OnLoadComplete?.Invoke();
            yield break;
        }

        asyncLoad.allowSceneActivation = false;

        float visibleTime = 0f;
        while (asyncLoad.progress < 0.9f || visibleTime < 0.45f)
        {
            visibleTime += Time.unscaledDeltaTime;
            // Reserve the final 5% for activating the destination scene.
            float normalizedProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f) * 0.95f;
            OnLoadProgress?.Invoke(normalizedProgress);

            yield return null;
        }

        OnLoadProgress?.Invoke(0.95f);
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
            yield return null;

        OnLoadProgress?.Invoke(1f);
        IsLoading = false;
        OnLoadComplete?.Invoke();
    }
}
