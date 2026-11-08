using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds small runtime UI to gameplay scenes without editing them:
///  - Scenes with the GameplayLoopController (Union Prime): its GameplayHUD already has the results and PAUSE,
///    so only the speed controller is added; GameplayHUD builds its button on the shared canvas.
///  - Older level scenes (LevelPlanetDefense with the PlanetHUD): a win / lose screen (GameResultScreen) and
///    working PAUSE / 1x-2x buttons on the existing HUD (LevelHudButtons).
/// </summary>
public static class GameplayBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach(); // the scene that was open when Play was pressed
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach();
    }

    static void Attach()
    {
        GameplayLoopController loop = Object.FindAnyObjectByType<GameplayLoopController>();
        if (loop != null)
        {
            if (loop.GetComponent<BattleSpeedButton>() == null) loop.gameObject.AddComponent<BattleSpeedButton>();
            return;
        }
        LevelPlanetDefense defense = Object.FindAnyObjectByType<LevelPlanetDefense>();
        if (defense == null) return; // menus, level select...
        GameObject host = defense.gameObject;
        if (host.GetComponent<GameResultScreen>() == null) host.AddComponent<GameResultScreen>();
        if (host.GetComponent<LevelHudButtons>() == null) host.AddComponent<LevelHudButtons>();
    }
}
