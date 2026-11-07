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
    public LevelSelectDetails destinationDetails;
    public Image[] destinationHalos = new Image[0];
    public Text[] destinationLabels = new Text[0];
    public Color destinationLabelColor = new Color32(155, 183, 218, 255);
    public Color selectedLabelColor = new Color32(255, 239, 195, 255);

    private bool changingScene;

    private void Start()
    {
        RefreshDestinationSelection();
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
        RefreshDestinationSelection();
        AudioManager.Instance.PlaySFX(confirmSound, 0.65f);
    }

    // Confirmed selection is independent of the EventSystem's hover/keyboard focus.
    public void RefreshDestinationSelection()
    {
        if (selectedDestination == null) return;

        for (int i = 0; i < destinationLabels.Length; i++)
        {
            bool selected = destinationLabels[i].text == selectedDestination.text;
            destinationHalos[i].enabled = selected;
            destinationLabels[i].color = selected ? selectedLabelColor : destinationLabelColor;
            if (selected && destinationDetails != null)
                destinationDetails.ShowDestination(i);
        }
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
