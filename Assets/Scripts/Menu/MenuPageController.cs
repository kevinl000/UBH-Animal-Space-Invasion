using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuPageController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip confirmSound;
    public AudioClip backSound;
    public Button firstButton;
    public CanvasGroup controls;
    public Text selectedDestination;

    private bool changingScene;

    private void Start()
    {
        if (firstButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }

    private void Update()
    {
        if (selectedDestination != null && Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame)
            OpenMainPage();
    }

    public void OpenLevelsPage()
    {
        Navigate("LevelSelect", confirmSound);
    }

    public void OpenMainPage()
    {
        Navigate("MainMenu", backSound);
    }

    public void SelectDestination(string destination)
    {
        if (changingScene || SceneLoadingScreen.IsLoading || selectedDestination == null)
            return;

        selectedDestination.text = destination;
        audioSource.PlayOneShot(confirmSound, 0.65f);
    }

    private void Navigate(string sceneName, AudioClip sound)
    {
        if (changingScene || SceneLoadingScreen.IsLoading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Menu scene is missing from the build: " + sceneName, this);
            return;
        }

        changingScene = true;
        controls.interactable = false;
        StartCoroutine(ChangeScene(sceneName, sound));
    }

    private IEnumerator ChangeScene(string sceneName, AudioClip sound)
    {
        // Keep this scene alive briefly so its confirmation sound can be heard.
        if (sound != null)
        {
            audioSource.PlayOneShot(sound);
            yield return new WaitForSecondsRealtime(Mathf.Min(sound.length, 0.3f));
        }

        if (SceneLoadingScreen.Load(sceneName))
            while (SceneLoadingScreen.IsLoading)
                yield return null;

        // Reached only if loading failed and this menu is still alive.
        changingScene = false;
        controls.interactable = true;
    }
}
