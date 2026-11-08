using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Makes the HUD's PAUSE and "1x / 2x" buttons work (they had no click handlers).
/// Finds them by name in the scene, so the HUD prefab doesn't change. Added at runtime by GameplayBootstrap.
/// </summary>
public class LevelHudButtons : MonoBehaviour
{
    public string pauseButtonName = "PauseButton";
    public string speedButtonName = "SpeedButton";
    public float fastSpeed = 2f;

    public bool Paused { get; private set; }
    public float Speed { get; private set; } = 1f;

    private Text pauseLabel;
    private Text speedLabel;

    private void OnEnable() { GameManager.OnGameStateChanged += OnState; }
    private void OnDisable() { GameManager.OnGameStateChanged -= OnState; }

    private void Start()
    {
        Button pause = FindButton(pauseButtonName);
        Button speed = FindButton(speedButtonName);
        if (pause != null)
        {
            Position(pause, 86f);
            pause.onClick.AddListener(TogglePause);
            pauseLabel = pause.GetComponentInChildren<Text>();
        }
        if (speed != null)
        {
            Position(speed, 244f);
            speed.onClick.AddListener(ToggleSpeed);
            speedLabel = speed.GetComponentInChildren<Text>();
        }
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterLoadedLevel(SceneManager.GetActiveScene().name);
        Refresh();
    }

    private static void Position(Button button, float x)
    {
        var rt = button.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        // Leave the existing planet title and resource strip above the controls.
        rt.anchoredPosition = new Vector2(x, -132f);
        rt.sizeDelta = new Vector2(140f, 40f);
    }

    private static Button FindButton(string name)
    {
        foreach (Button b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (b.gameObject.name == name) return b;
        return null;
    }

    private static bool Ended()
    {
        GameManager gm = GameManager.Instance;
        return gm != null && (gm.CurrentState == GameManager.GameState.GameOver || gm.CurrentState == GameManager.GameState.LevelComplete);
    }

    public void TogglePause()
    {
        if (Ended() || SceneLoadingScreen.IsLoading) return;
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            if (gm.CurrentState == GameManager.GameState.Paused) gm.ResumeGame();
            else gm.PauseGame();
        }
        else
        {
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : Speed;
        }
        Refresh();
    }

    private void OnState(GameManager.GameState state)
    {
        Paused = state == GameManager.GameState.Paused;
        if (state == GameManager.GameState.Playing) Time.timeScale = Speed;
        Refresh();
    }

    public void ToggleSpeed()
    {
        if (Ended() || Paused || SceneLoadingScreen.IsLoading) return;
        Speed = Speed > 1f ? 1f : fastSpeed;
        if (!Paused) Time.timeScale = Speed;
        Refresh();
    }

    private void Refresh()
    {
        if (pauseLabel != null) pauseLabel.text = Paused ? "RESUME" : "PAUSE";
        if (speedLabel != null) speedLabel.text = Speed > 1f ? "2x  >>" : "1x  >";
    }

    private void OnDestroy()
    {
        if (!Ended()) Time.timeScale = 1f; // never leave the next scene paused or fast
    }
}
