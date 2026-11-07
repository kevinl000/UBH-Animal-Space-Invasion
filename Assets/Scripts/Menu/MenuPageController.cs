using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuPageController : MonoBehaviour
{
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
        Navigate(true, confirmSound);
    }

    public void OpenMainPage()
    {
        Navigate(false, backSound);
    }

    public void SelectDestination(string destination)
    {
        if (changingScene || SceneLoadingScreen.IsLoading || selectedDestination == null)
            return;

        selectedDestination.text = destination;
        AudioManager.Instance.PlaySFX(confirmSound, 0.65f);
    }

    private void Navigate(bool openLevelSelect, AudioClip sound)
    {
        if (changingScene || SceneLoadingScreen.IsLoading)
            return;

        if (openLevelSelect)
            GameManager.Instance.GoToLevelSelect();
        else
            GameManager.Instance.GoToMainMenu();

        if (!SceneController.Instance.IsLoading)
            return;

        changingScene = true;
        controls.interactable = false;
        AudioManager.Instance.PlaySFX(sound);
    }

    private void OnEnable()
    {
        SceneController.OnLoadComplete += RestoreControls;
    }

    private void OnDisable()
    {
        SceneController.OnLoadComplete -= RestoreControls;
    }

    private void RestoreControls()
    {
        changingScene = false;
        controls.interactable = true;
    }
}
