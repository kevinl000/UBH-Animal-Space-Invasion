using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private float targetProgress;
    private float shownProgress;
    private EventSystem blockedEventSystem;
    private bool previousNavigationEvents;

    public static bool IsLoading => instance != null
        || (SceneController.Instance != null && SceneController.Instance.IsLoading);

    // Compatibility entry point; SceneController owns every scene load.
    public static bool Load(string sceneName)
    {
        if (IsLoading)
            return false;

        return SceneController.Instance.TryLoadScene(sceneName);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterLoadingEvents()
    {
        SceneController.OnLoadBegin -= Show;
        SceneController.OnLoadProgress -= UpdateProgress;
        SceneController.OnLoadComplete -= Complete;
        SceneController.OnLoadBegin += Show;
        SceneController.OnLoadProgress += UpdateProgress;
        SceneController.OnLoadComplete += Complete;
    }

    private static void Show()
    {
        if (instance == null)
        {
            var prefab = Resources.Load<SceneLoadingScreen>("SceneLoadingScreen");
            if (prefab == null)
            {
                Debug.LogError("The SceneLoadingScreen prefab is missing from Resources.");
                return;
            }

            instance = Instantiate(prefab);
            DontDestroyOnLoad(instance.gameObject);
        }

        // A new manager request can arrive while the previous overlay is fading.
        instance.StopAllCoroutines();
        instance.canvasGroup.alpha = 1;
        instance.targetProgress = 0;
        instance.shownProgress = 0;
        instance.status.text = "LOADING...";
        instance.SetProgress(0);
        instance.BlockNavigation();
    }

    private static void UpdateProgress(float progress)
    {
        if (instance == null)
            return;

        instance.targetProgress = progress;
        if (progress >= 1f)
        {
            instance.shownProgress = 1f;
            instance.SetProgress(1f);
            instance.status.text = "READY!";
        }
        else if (progress >= 0.95f)
            instance.status.text = "ARRIVING...";
    }

    private static void Complete()
    {
        if (instance != null)
            instance.StartCoroutine(instance.FadeOut());
    }

    private void Update()
    {
        shownProgress = Mathf.MoveTowards(shownProgress, targetProgress, Time.unscaledDeltaTime * 3);
        SetProgress(shownProgress);
        BlockNavigation();
    }

    private IEnumerator FadeOut()
    {
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
