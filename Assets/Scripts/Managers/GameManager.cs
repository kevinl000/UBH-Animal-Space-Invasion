using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<GameState> OnGameStateChanged;

    public enum GameState
    {
        Menu,           // Active during Main Menu and Level Select
        Playing,        // Active during standard gameplay
        Paused,         // Active when the player pauses the game
        LevelComplete,  // Active when the player beats the level
        GameOver        // Active when the player loses
    }

    public GameState CurrentState { get; private set; }
    public string LastPlayedLevel { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void UpdateGameState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
    public void GoToMainMenu()
    {
        if (!SceneController.Instance.TryLoadScene("MainMenu")) return;

        Time.timeScale = 1f; // Always reset time scale when leaving a level!
        UpdateGameState(GameState.Menu);
    }

    public void GoToLevelSelect()
    {
        if (!SceneController.Instance.TryLoadScene("LevelSelect")) return;

        Time.timeScale = 1f;
        UpdateGameState(GameState.Menu);
    }

    public void StartLevel(string levelName)
    {
        if (!SceneController.Instance.TryLoadScene(levelName)) return;

        Time.timeScale = 1f;
        LastPlayedLevel = levelName; // Remember this so we can implement a "Restart" button easily
        UpdateGameState(GameState.Playing);
    }

    public void RestartCurrentLevel()
    {
        if (!string.IsNullOrEmpty(LastPlayedLevel))
        {
            StartLevel(LastPlayedLevel);
        }
        else
        {
            GoToLevelSelect(); // Fallback in case something goes wrong
        }
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return; // Only pause if we are actually playing

        Time.timeScale = 0f; // Freeze physics and updates
        UpdateGameState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;

        Time.timeScale = 1f;
        UpdateGameState(GameState.Playing);
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        UpdateGameState(GameState.GameOver);
    }

    public void LevelComplete()
    {
        Time.timeScale = 0f; // Freeze the game in the background while victory UI shows
        UpdateGameState(GameState.LevelComplete);

        // You could also save high scores or unlock the next level here
    }
}
