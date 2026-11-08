using UnityEngine;
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
    public float Speed { get; private set; }

    private Text pauseLabel;
    private Text speedLabel;

    private void Start()
    {
        Speed = 1f;
        Button pause = FindButton(pauseButtonName);
        Button speed = FindButton(speedButtonName);
        if (pause != null) { pause.onClick.AddListener(TogglePause); pauseLabel = pause.GetComponentInChildren<Text>(); }
        if (speed != null) { speed.onClick.AddListener(ToggleSpeed); speedLabel = speed.GetComponentInChildren<Text>(); }
        Refresh();
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
        if (Ended()) return;
        Paused = !Paused;
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.CurrentState == GameManager.GameState.Playing && Paused) gm.PauseGame();
        else if (gm != null && gm.CurrentState == GameManager.GameState.Paused && !Paused) gm.ResumeGame();
        Time.timeScale = Paused ? 0f : Speed; // also when the level was opened directly (no "Playing" state)
        Refresh();
    }

    public void ToggleSpeed()
    {
        if (Ended()) return;
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
