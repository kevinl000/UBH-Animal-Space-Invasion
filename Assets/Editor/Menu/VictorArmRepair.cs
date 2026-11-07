using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Temporary editor repair for the menu scene that was open during the sprite replacement.
[InitializeOnLoad]
public static class VictorArmRepair
{
    private static readonly string Request = Path.GetFullPath(Application.dataPath + "/../.git/menu-work/victor-arm-repair-request");
    private static readonly string Result = Path.GetFullPath(Application.dataPath + "/../.git/menu-work/victor-arm-repair-result.txt");

    static VictorArmRepair()
    {
        EditorApplication.delayCall += Repair;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += Repair;
    }

    private static void Repair()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity" && scene.path != "Assets/Scenes/LevelSelect.unity")
        {
            File.WriteAllText(Result, "Skipped: active scene is " + scene.path);
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetBool("VictorArmRepair.ResumePreview", true);
            EditorApplication.isPlaying = false;
            return;
        }

        int removed = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "RightArm" || child.parent == null || child.parent.name != "Victor")
                    continue;
                Undo.DestroyObjectImmediate(child.gameObject);
                removed++;
            }
        }

        // The corrected scene is already on disk; preserve any unrelated unsaved editor changes.
        File.WriteAllText(Result, "Removed " + removed + " raised-arm object(s) from the open " + scene.name + " scene.");
        File.Delete(Request);
        if (SessionState.GetBool("VictorArmRepair.ResumePreview", false))
        {
            SessionState.EraseBool("VictorArmRepair.ResumePreview");
            EditorApplication.isPlaying = true;
        }
    }
}
